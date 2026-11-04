"""Private W-166 card-layout study. Does not write runtime assets or card definitions."""
from pathlib import Path
import math
from PIL import Image, ImageDraw, ImageFont, ImageOps

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Creative/card-redesign-v1'
OUT.mkdir(parents=True, exist_ok=True)
S = 3
INK = '#1b2931'
CREAM = '#f0e6cc'
GOLD = '#cbbb95'

def font(n, bold=False):
    return ImageFont.truetype('C:/Windows/Fonts/' + ('arialbd.ttf' if bold else 'arial.ttf'), round(n*S))

def center(d, x, y, text, n, color, bold=False):
    d.text((x*S,y*S),str(text),font=font(n,bold),fill=color,anchor='mm')

# Values and wording transcribed from Core/Cards/Verified/<Hero>CardContent.cs;
# Cantrip wording is owned by GydionCardContent.Actions.cs. Zero secondary
# Movement is a printed dash; Rowenna's primary Movement 2 stays distinct.
CARDS = [
    ('BROGAN','ram-warrior','Mad Dash',7,7,3,'ATTACK  6','#9b4740',
     'Before the attack: Move 2 spaces in a straight line to a space adjacent to an enemy unit, then target that unit. ( If you cannot make this move, you cannot attack. )','RED I',''),
    ('RIKI','cat-duelist','Sudden Breeze',10,5,1,'SKILL','#426d9b',
     'Move up to 3 spaces in a straight line, ignoring obstacles; you may place an enemy unit you moved through into a space adjacent to you.','BLUE I',''),
    ('ROWENNA','knight','Close Quarters',4,3,0,'MOVEMENT  2','#507347',
     'After movement, if you are adjacent to an enemy hero, you may Choose one - (1) Place a friendly minion in radius into a space adjacent to that enemy hero. (2) Place an enemy minion in radius into a space adjacent to you.','GREEN I','RADIUS 2'),
    ('TIGERCLAW','goblin','Blend Into Shadows',6,2,0,'BASIC SKILL','#78868b',
     'If you are adjacent to terrain, place yourself into a space in radius; if you do, Next turn: You are immune to enemy attack actions.','SILVER','RADIUS 2'),
    ('GYDION','grandmother-cat','Cantrip',11,1,0,'BASIC SKILL','#a17f2e',
     'Choose and cast one spell in the spellbook — • "Magic Missile" (To blast at short range!) • "Expeditious Retreat" (To dart a great distance!) • "Shocking Grasp" (To repel an adjacent assailant!)','GOLD',''),
    ('TALI','frog-wizard','Reign of Winter',0,0,0,'ULTIMATE','#46545b',
     'Each time after you defeat a minion with your basic attack, an enemy hero in radius discards a card of the color of your choice, if able.','ULTIMATE','RADIUS 4')
]

def wrap(d,text,n,width):
    lines=[]
    for para in text.replace(' • ','\n• ').split('\n'):
        line=''
        for word in para.split():
            candidate=(line+' '+word).strip()
            if d.textlength(candidate,font=font(n)) > width*S and line:
                lines.append(line);line=word
            else: line=candidate
        lines.append(line)
    return lines

def card(data):
    hero,asset,title,init,defense,move,primary,color,text,tier,radius=data
    im=Image.new('RGB',(372*S,520*S),INK);d=ImageDraw.Draw(im)
    def rounded(box,r,**kw): d.rounded_rectangle(tuple(round(v*S) for v in box),r*S,**kw)
    rounded((3,3,369,517),15,fill=INK,outline=GOLD,width=2*S)
    rounded((73,14,356,61),7,fill=CREAM)
    title_n=23
    while d.textlength(title,font=font(title_n,True))>263*S: title_n-=1
    center(d,214,38,title,title_n,INK,True)
    rounded((74,70,356,301),8,fill=color)
    art=Image.open(ROOT/f'Art/ConceptPortraits/{asset}-portrait-v1.png').convert('RGBA')
    art=art.crop(art.getbbox())
    art=ImageOps.contain(art,(270*S,223*S),Image.Resampling.LANCZOS)
    im.paste(art,(round(215*S-art.width/2),round(299*S-art.height)),art)
    d=ImageDraw.Draw(im)
    # Shared cream outline and large centred numbers; no text glyph icons.
    d.ellipse((9*S,17*S,67*S,75*S),fill=color,outline=CREAM,width=2*S)
    for tick in range(12):
        a=math.radians(tick*30-90);r=25;ri=20 if tick%3==0 else 22
        d.line(((38+ri*math.cos(a))*S,(46+ri*math.sin(a))*S,
                (38+r*math.cos(a))*S,(46+r*math.sin(a))*S),fill=CREAM,width=S)
    center(d,38,46,init or '–',29,CREAM,True)
    center(d,38,87,'INIT',9,GOLD,True)
    # Curved shield polygon sampled from a quadratic Bezier contour.
    pts=[(13,112),(38,106),(63,112),(62,137)]
    for i in range(1,21):
        t=i/20;pts.append(((1-t)**2*62+2*(1-t)*t*62+t*t*38,
                         (1-t)**2*137+2*(1-t)*t*158+t*t*171))
    for x,y in list(reversed(pts[3:])): pts.append((76-x,y))
    pts.append((13,112))
    points=[(round(x*S),round(y*S)) for x,y in pts]
    d.polygon(points,fill=color);d.line(points,fill=CREAM,width=2*S,joint='curve')
    center(d,38,134,defense or '–',30,CREAM,True)
    center(d,38,185,'DEF',9,GOLD,True)
    # Broad diagonal arrow leaves its central field clear for the value.
    pts=[(11,251),(31,231),(20,220),(65,209),(65,254),(53,242),(33,263),(11,251)]
    points=[(x*S,y*S) for x,y in pts]
    d.polygon(points,fill=color);d.line(points,fill=CREAM,width=2*S,joint='curve')
    center(d,42,236,move or '–',27,CREAM,True)
    center(d,38,282,'MOVE',9,GOLD,True)
    rounded((15,312,356,502),8,fill=CREAM)
    center(d,185,333,primary,18,INK,True)
    d.line((28*S,350*S,343*S,350*S),fill=GOLD,width=S)
    n=15
    while True:
        lines=wrap(d,text,n,311)
        if len(lines)*(n+3)<=117: break
        n-=0.5
        assert n>=11, 'Rules panel overflow'
    for i,line in enumerate(lines):
        d.text((29*S,(361+i*(n+3))*S),line,font=font(n),fill=INK)
    if radius:
        rounded((27,480,101,495),4,fill=color)
        center(d,64,488,radius,9,CREAM,True)
    center(d,266,488,tier,9,'#596567',True)
    return im.resize((372,520),Image.Resampling.LANCZOS)

sheet=Image.new('RGB',(2420,1060),'#101a25');d=ImageDraw.Draw(sheet)
def label(x,y,text,n,color=CREAM):
    f=ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',n)
    d.text((x,y),text,font=f,fill=color,anchor='mm')
label(1210,38,'MEGDAN  /  CARD DESIGN STUDY',28)
label(1210,72,'Larger character art · cream panels · clearer shield and movement silhouettes',17,'#a9bdc8')
for i,data in enumerate(CARDS):
    face=card(data);x=24+i*400
    label(x+186,112,data[0],16,GOLD)
    sheet.paste(face,(x,135))
    face.save(OUT/(data[1]+'.png'))
    sheet.paste(face.resize((223,312),Image.Resampling.LANCZOS),(x+74,704))
label(1210,681,'HAND-SIZE CHECK  /  223 × 312 px',15,'#a9bdc8')
label(1210,1040,'PRIVATE VISUAL PREVIEW · EXISTING CARD VALUES AND WORDING · NOT RUNTIME',13,'#a9bdc8')
sheet.save(OUT/'six-heroes-preview.png')
print(f'Rendered six native-aspect faces plus hand-size row: {OUT}')
