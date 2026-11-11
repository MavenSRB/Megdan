# MVP animation and audio budget feasibility

Return to [Smart Start](SMART_START.md) and the [production inventory](SMART_START_PRODUCTION_INVENTORY.md). W-199 desk research, 11 November 2026. This assesses the applicant's six-hero MVP budget, not the approximately twenty-hero commercial launch. It does not adopt a budget change, select a supplier, authorize purchases/contact, or define final hero mechanics.

## Conclusion and budget arithmetic

The RSD 30,000 stock-audio allowance is credible for a functional MVP when Stefan supplies selection, editing and integration through his existing labour allocation. The RSD 835,000 rigging/animation envelope is plausible for a tightly specified freelance package with compatible rigs, reused/adapted core motions and a few bespoke actions. It is not yet a quotation-backed promise that every character can receive separately authored motion sets. No reviewed source offers a binding price for Megdan's actual models, skinning, source files, revisions and rights terms.

The supplied table sums correctly: human resources 4,590,000 + business support 460,000 + equipment 85,000 + external services 865,000 = RSD 6,000,000; mentorship adds 600,000 for RSD 6,600,000. External services split correctly into animation 835,000 and audio 30,000. Human resources are 76.5% of project costs. No general contingency remains. This is an arithmetic/production review, not a fresh determination of Fund eligibility or founder payroll/tax compliance.

For indicative conversion only, the [NBS page retrieved during research](https://webappcenter.nbs.rs/ExchangeRateWebApp/ExchangeRate/CurrentMiddleRate) displayed the **4 November 2026** list, EUR 117.6013 and USD 104.7020 RSD. This cached list is not represented as the 11 November spot rate. Animation is approximately EUR 7,100 / USD 7,975; audio EUR 255 / USD 287. Supplier fees, exchange spreads and applicable taxes must be included in the eventual cash requirement; the application categories are excluding VAT.

## What must be priced for this MVP

Veljko supplies models/textures; Stefan integrates. The external package covers six hero and three minion character deliveries, including rigging/skinning and useful animation. Nine deliveries need not require nine independently designed skeletons or nine sets of authored source motions. Shared rig families still need per-mesh skinning and deformation checks.

The current inventory calls for six basic hero states (idle, movement, attack/action, hit, defeat, return) and five minion states (idle, movement, attack, hit, defeat): **51 character-state assignments**, before signature actions. This is neither 51 mandatory bespoke clips nor one clip per card. Return can use a spawn effect and existing motion; multiple abilities may share a cast motion plus VFX; compatible bodies can reuse locomotion. These are costing options, not changes to approved gameplay or final art decisions.

MVP acceptance means readable silhouettes, clear movement/attack/defeat timing and stable Unity playback. It does not imply facial performance, cinematics, elaborate cloth simulation, independent animation per card or premium signature variety. VFX remains Veljko's existing work package; an animator quote must not silently be assumed to cover it.

## Published animation price evidence

| Primary source | Published offer | Relevance and limitation |
| --- | --- | --- |
| [Pixune game animation](https://pixune.com/game-animation-studio/) | USD 200–1,000 per 2–4 second 3D cycle; USD 500–2,500 for 3D rigging | Supplier guide, not a Megdan quote. Engine requirements, complexity and revisions affect price. |
| [RocketBrush pricing](https://rocketbrush.com/blog/game-art-outsourcing-prices-complete-pricing-breakdown) | USD 200–1,000 per quick 2–3 second cycle; basic/mid rigs USD 500–2,000 | May 2025 guide still accessible; age and scope limit precision. Independently supports the same order of magnitude. |
| [Psalmseen's own freelance listing](https://www.fiverr.com/psalmseen/model-3d-character-rig-and-animate) | Description advertises rigging USD 100; animation USD 200 per 15 seconds, separately from modelling | Demonstrates lower advertised freelance entry points, not verified game quality, current capacity, a batch discount or confirmed skinning/source/rights coverage. Platform checkout may show another currency; fees are extra unless included. |
| [Adobe Mixamo FAQ](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html) | Free with Adobe ID; royalty-free game use; humanoid rigging and motion library | A possible baseline-motion source. Distorted proportions, tails, wings, clothing and props can defeat auto-rigging. Cleanup, weapon grip, contact, looping and Unity acceptance still cost labour. Licensed motions cannot be represented as newly commissioned exclusive work. |

A deliberately separate bespoke-studio scenario shows the constraint: nine rigs at USD 500 plus 51 newly authored cycles at USD 200 equals USD 14,700, approximately **RSD 1,539,119**, before signature work or revisions. This is a conditional calculation at published low endpoints, not an MVP minimum or a quote; it deliberately assumes no animation reuse. It explains why that purchasing approach does not fit RSD 835,000.

For the lean approach, retain approximately 15% inside the animation envelope for corrections and delivery issues: RSD 125,250, leaving RSD 709,750 (about EUR 6,035) for the defined base order. At **assumed**, not researched supplier offers of EUR 25/35/50 per hour, that base buys approximately 241/172/121 hours. This is useful bid sensitivity, not an estimate of required effort. Procurement should ask for a fixed scoped price rather than assume those hours suffice.

The feasible route is to quote compatible rig families, skinning of all nine final models, reused or licensed baseline motion, weapon/cast adaptations and a small named set of signature clips together. Use roughly one distinctive action per hero as an initial quotation option, with variants priced separately; final requirements follow the action-presentation matrix. Bespoke complexity, a second body in one hero, unusual appendages or poor topology can consume the savings. Do not force a creative redesign merely to satisfy this estimate.

## Stock audio evidence and sample basket

| Primary source | Price/content inspected | Limits |
| --- | --- | --- |
| [Gamemaster Pro Sound Collection](https://www.gamemasteraudio.com/product/pro-sound-collection/) | USD 47, 8,076 effects, including magic/combat/foley/UI/ambience categories | Broad library, not a verified match to every event or style. [EULA](https://gamemasteraudio.com/wp-content/uploads/2020/01/Gamemaster-Audio-End-User-License-Agreement.pdf) permits commercial game use and editing; raw access/editing is single-user, nontransferable, and requires purchase proof. Embedded project access differs from raw editing. |
| [Ovani Fantasy Music Vol. 1](https://ovanisound.com/products/fantasy-music-pack-vol-1) | USD 50; 10 tracks with variants; commercial-use listing | Review full licence at procurement; confirm looping in the intended pipeline without assuming a paid integration plugin is included. |
| [Ovani fantasy catalogue](https://ovanisound.com/collections/fantasy) | Medieval fantasy and simple magic effects packs listed at USD 20 each | Illustrative gap-fill packs; exact selection requires previews and the audio event map. |

Illustrative basket: USD 47 + 50 + 20 + 20 = **USD 137**, approximately **RSD 14,344**, leaving roughly RSD 15,656 under the stock-asset ceiling before any extra fees/tax cash requirement. Even adding another USD 47 effects licence for a second raw-audio editor gives about RSD 19,265 total. This establishes price feasibility, not final artistic acceptance or a purchase recommendation. A broad library can provide most cues without buying a pack for every hero.

No audio contractor, original score, custom recording or voice production is funded by RSD 30,000. The meaningful workload is mapping events, finding coherent cues, trimming/layering, loudness, loop/transitions, simultaneous-sound control and testing. That labour already belongs to Stefan; allow schedule capacity instead of claiming the licence purchase delivers finished audio. Preserve company invoices/licences and verify the eventual publisher/team access model. Do not assume licence ownership transfers with commissioned-animation ownership.

## Recommendation and closing procurement check

Keep the two figures provisionally for the MVP. Describe animation as **MVP rigging, skinning and essential reusable gameplay animation, including limited hero-specific actions, source delivery and Unity acceptance**. Keep sound explicitly as **licensed stock music and effects, selected and integrated by the team**. Neither statement commits to new content counts or authorizes purchasing.

The remaining decisive check is an itemized supplier quote against actual representative meshes: all nine characters, core-state mapping, reused versus bespoke clips, skinning/deformation, weapon attachments, export scale/axes, source project format, delivery schedule, named revision rounds, repair acceptance, rights for original commissioned work, licences for reused materials and total excluding VAT/fees. Include a representative hero pilot within the same envelope and test it in Unity before the remainder is produced. Source files plus rights assignment for every reused library motion must not be demanded as though the animator owns that library exclusively.

If the scoped quote fits below the base-order target with corrections retained, the animation figure becomes defensible. If it does not, the decision is to simplify presentation or reallocate a quoted amount within the overall budget; this research does not authorize either change. A general budget increase based only on premium studio rates is not justified for this MVP. No supplier was contacted and no asset was purchased or auditioned in the game.
