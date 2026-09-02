using System;
using System.Security.Cryptography;
using System.Text;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.32. The pre-shared-key AEAD frame layer: every session frame after a verified
    /// key proof travels sealed - encrypted and authenticated - so an on-path observer of the link
    /// can neither read an owner-private byte nor inject a frame the peer would act on.
    /// </summary>
    /// <remarks>
    /// <para>This is the encryption decision the amended architecture record owed at this
    /// milestone, resolved for the pre-shared-key layer over <c>SslStream</c> with a pinned
    /// certificate: the session key and the per-connection random challenge nonce already exist,
    /// so no certificate has to be generated, distributed or pinned; the layer is deterministic
    /// given its inputs and testable without a socket; and it sits at the frame boundary, so the
    /// single-threaded pump discipline and the bounded assembler are untouched.</para>
    /// <para>Keys are derived per connection with HKDF-SHA256 from the session key and the host's
    /// random challenge nonce, one encryption and one authentication key per direction, so a
    /// captured frame cannot be replayed into the other direction or into another connection. The
    /// cipher is AES-256 in counter mode keyed by the frame's own strictly monotonic sequence,
    /// authenticated encrypt-then-MAC with HMAC-SHA256 over the sequence and ciphertext; a frame
    /// whose MAC fails, whose sequence is not exactly the next one, or whose inner kind is invalid
    /// is refused whole, and a refusal never advances the receive sequence.</para>
    /// <para>An unkeyed session has no key to derive from and stays plaintext; the launch options
    /// refuse a non-loopback bind without a session key, so an owner-private byte can cross
    /// plaintext only on the loopback interface, where the record's on-path observer does not
    /// exist. Reject and Goodbye stay readable in plaintext even on a sealed channel: they carry
    /// no secret, and a pre-admission refusal has no keys yet - the residual, that an on-path
    /// party can forge a close signal, is exactly the denial of service it could already commit
    /// with a reset, and is recorded in the decision record.</para>
    /// </remarks>
    public sealed class PrivateSessionSecureChannel
    {
        /// <summary>Sequence (8) + HMAC-SHA256 (32) + inner kind (2).</summary>
        public const int SealOverheadBytes = 8 + 32 + 2;

        /// <summary>The largest inner payload a sealed frame can carry.</summary>
        public const int MaxSealedPayloadBytes =
            PrivateSessionProtocol.MaxPayloadBytes - SealOverheadBytes;

        private const int MacBytes = 32;
        private const int KeyBytes = 32;

        private readonly byte[] sendCipherKey;
        private readonly byte[] sendMacKey;
        private readonly byte[] receiveCipherKey;
        private readonly byte[] receiveMacKey;
        private long sendSequence;
        private long receiveSequence;

        private PrivateSessionSecureChannel(
            byte[] sendCipherKey, byte[] sendMacKey, byte[] receiveCipherKey, byte[] receiveMacKey)
        {
            this.sendCipherKey = sendCipherKey;
            this.sendMacKey = sendMacKey;
            this.receiveCipherKey = receiveCipherKey;
            this.receiveMacKey = receiveMacKey;
        }

        /// <summary>The host's end: seals host-to-client, unseals client-to-host.</summary>
        public static PrivateSessionSecureChannel ForHost(string sessionKey, byte[] challengeNonce)
        {
            var hostToClient = DeriveDirection(sessionKey, challengeNonce, "host-to-client");
            var clientToHost = DeriveDirection(sessionKey, challengeNonce, "client-to-host");
            return new PrivateSessionSecureChannel(
                hostToClient.cipher, hostToClient.mac, clientToHost.cipher, clientToHost.mac);
        }

        /// <summary>The client's end: seals client-to-host, unseals host-to-client.</summary>
        public static PrivateSessionSecureChannel ForClient(string sessionKey, byte[] challengeNonce)
        {
            var hostToClient = DeriveDirection(sessionKey, challengeNonce, "host-to-client");
            var clientToHost = DeriveDirection(sessionKey, challengeNonce, "client-to-host");
            return new PrivateSessionSecureChannel(
                clientToHost.cipher, clientToHost.mac, hostToClient.cipher, hostToClient.mac);
        }

        /// <summary>
        /// Seals one inner frame into a complete <see cref="SessionMessageKind.Sealed"/> frame.
        /// Throws on misuse - an unbounded payload is a caller defect, never wire input.
        /// </summary>
        public byte[] Seal(SessionMessageKind kind, byte[] payload)
        {
            payload = payload ?? Array.Empty<byte>();
            if (kind == SessionMessageKind.None || kind == SessionMessageKind.Sealed)
            {
                throw new ArgumentException("A sealed frame carries exactly one real inner kind.", nameof(kind));
            }

            if (payload.Length > MaxSealedPayloadBytes)
            {
                throw new ArgumentException("A sealed payload is bounded.", nameof(payload));
            }

            sendSequence++;
            var plaintext = new byte[2 + payload.Length];
            plaintext[0] = (byte)((int)kind & 0xFF);
            plaintext[1] = (byte)(((int)kind >> 8) & 0xFF);
            Array.Copy(payload, 0, plaintext, 2, payload.Length);

            var ciphertext = ApplyCounterCipher(sendCipherKey, sendSequence, plaintext);
            var sealedPayload = new byte[8 + ciphertext.Length + MacBytes];
            WriteInt64(sealedPayload, 0, sendSequence);
            Array.Copy(ciphertext, 0, sealedPayload, 8, ciphertext.Length);
            var mac = ComputeMac(sendMacKey, sealedPayload, 8 + ciphertext.Length);
            Array.Copy(mac, 0, sealedPayload, 8 + ciphertext.Length, MacBytes);
            return PrivateSessionFrameCodec.EncodeFrame(SessionMessageKind.Sealed, sealedPayload);
        }

        /// <summary>
        /// Unseals one received <see cref="SessionMessageKind.Sealed"/> payload. Refuses - without
        /// advancing the receive sequence - a short payload, a sequence that is not exactly the
        /// next one, a failed MAC, or an invalid inner kind.
        /// </summary>
        public bool TryUnseal(byte[] sealedPayload, out SessionFrame inner)
        {
            inner = default;
            if (sealedPayload == null || sealedPayload.Length < SealOverheadBytes)
            {
                return false;
            }

            var sequence = ReadInt64(sealedPayload, 0);
            if (sequence != receiveSequence + 1)
            {
                return false;
            }

            var ciphertextLength = sealedPayload.Length - 8 - MacBytes;
            var expectedMac = ComputeMac(receiveMacKey, sealedPayload, 8 + ciphertextLength);
            if (!FixedTimeEquals(expectedMac, sealedPayload, 8 + ciphertextLength))
            {
                return false;
            }

            var ciphertext = new byte[ciphertextLength];
            Array.Copy(sealedPayload, 8, ciphertext, 0, ciphertextLength);
            var plaintext = ApplyCounterCipher(receiveCipherKey, sequence, ciphertext);
            var kindValue = plaintext[0] | (plaintext[1] << 8);
            if (!PrivateSessionFrameCodec.TryReadMessageKind(kindValue, out var kind) ||
                kind == SessionMessageKind.Sealed)
            {
                return false;
            }

            var payload = new byte[plaintext.Length - 2];
            Array.Copy(plaintext, 2, payload, 0, payload.Length);
            receiveSequence = sequence;
            inner = new SessionFrame(kind, payload);
            return true;
        }

        /// <summary>
        /// HKDF-SHA256 (RFC 5869): extract with the challenge nonce as salt, then expand with a
        /// per-direction info string, yielding one cipher key and one MAC key.
        /// </summary>
        private static (byte[] cipher, byte[] mac) DeriveDirection(
            string sessionKey, byte[] challengeNonce, string direction)
        {
            if (string.IsNullOrEmpty(sessionKey))
            {
                throw new ArgumentException(
                    "A secure channel requires a session key; an unkeyed session stays plaintext " +
                    "and is bound to the loopback interface by the launch options.",
                    nameof(sessionKey));
            }

            if (challengeNonce == null || challengeNonce.Length == 0)
            {
                throw new ArgumentException(
                    "A secure channel requires the connection's random challenge nonce.",
                    nameof(challengeNonce));
            }

            byte[] pseudoRandomKey;
            using (var extract = new HMACSHA256(challengeNonce))
            {
                pseudoRandomKey = extract.ComputeHash(Encoding.UTF8.GetBytes(sessionKey));
            }

            var info = Encoding.UTF8.GetBytes(
                PrivateSessionProtocol.FamilyName + "/" + PrivateSessionProtocol.Version +
                " seal " + direction);
            using (var expand = new HMACSHA256(pseudoRandomKey))
            {
                var block = Array.Empty<byte>();
                var output = new byte[KeyBytes * 2];
                var written = 0;
                byte counter = 1;
                while (written < output.Length)
                {
                    var input = new byte[block.Length + info.Length + 1];
                    Array.Copy(block, 0, input, 0, block.Length);
                    Array.Copy(info, 0, input, block.Length, info.Length);
                    input[input.Length - 1] = counter;
                    block = expand.ComputeHash(input);
                    var take = Math.Min(block.Length, output.Length - written);
                    Array.Copy(block, 0, output, written, take);
                    written += take;
                    counter++;
                }

                var cipher = new byte[KeyBytes];
                var mac = new byte[KeyBytes];
                Array.Copy(output, 0, cipher, 0, KeyBytes);
                Array.Copy(output, KeyBytes, mac, 0, KeyBytes);
                return (cipher, mac);
            }
        }

        /// <summary>
        /// AES-256 counter mode over the frame sequence: block <c>i</c> of frame <c>s</c> encrypts
        /// counter <c>s||i</c>, so no counter block ever repeats under one key - the sequence is
        /// strictly monotonic per direction and the block index is bounded by the frame.
        /// </summary>
        private static byte[] ApplyCounterCipher(byte[] key, long sequence, byte[] data)
        {
            var output = new byte[data.Length];
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                using (var encryptor = aes.CreateEncryptor())
                {
                    var counter = new byte[16];
                    var keystream = new byte[16];
                    WriteInt64(counter, 0, sequence);
                    for (var offset = 0; offset < data.Length; offset += 16)
                    {
                        WriteInt32(counter, 8, offset / 16);
                        encryptor.TransformBlock(counter, 0, 16, keystream, 0);
                        var take = Math.Min(16, data.Length - offset);
                        for (var index = 0; index < take; index++)
                        {
                            output[offset + index] = (byte)(data[offset + index] ^ keystream[index]);
                        }
                    }
                }
            }

            return output;
        }

        private static byte[] ComputeMac(byte[] key, byte[] buffer, int count)
        {
            using (var hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(buffer, 0, count);
            }
        }

        /// <summary>Compared over every byte, so a mismatch costs the same as a match.</summary>
        private static bool FixedTimeEquals(byte[] expected, byte[] buffer, int offset)
        {
            if (offset + expected.Length > buffer.Length)
            {
                return false;
            }

            var difference = 0;
            for (var index = 0; index < expected.Length; index++)
            {
                difference |= expected[index] ^ buffer[offset + index];
            }

            return difference == 0;
        }

        private static void WriteInt64(byte[] buffer, int offset, long value)
        {
            for (var index = 0; index < 8; index++)
            {
                buffer[offset + index] = (byte)((value >> (8 * index)) & 0xFF);
            }
        }

        private static void WriteInt32(byte[] buffer, int offset, int value)
        {
            for (var index = 0; index < 4; index++)
            {
                buffer[offset + index] = (byte)((value >> (8 * index)) & 0xFF);
            }
        }

        private static long ReadInt64(byte[] buffer, int offset)
        {
            var value = 0L;
            for (var index = 0; index < 8; index++)
            {
                value |= (long)buffer[offset + index] << (8 * index);
            }

            return value;
        }
    }
}
