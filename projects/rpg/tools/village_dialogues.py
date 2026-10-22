# Run from projects/rpg:  python3 tools/village_dialogues.py data
# One dialogue per new inhabitant of Clairval (sprint 104) and of the zones (sprint 105), from docs/textes-v5.md §3:
# the first line as the greeting, each other line behind a short question of the player's, and a goodbye.
import collections, json, sys
O = collections.OrderedDict
root = sys.argv[1]
def t(fr, en): return O([("fr", fr), ("en", en)])
BYE = t("À bientôt.", "See you.")
P = {
 "hortense": [
  (None, t("Attention aux pieux, ils sont frais.", "Mind the stakes, they're fresh.")),
  (t("Et la palissade ?", "And the palisade?"), t("Mille deux cents pieux, trois portes et pas un trou. Enfin, pas un trou que je n'aie pas vu.", "Twelve hundred stakes, three gates and not one gap. Well, not one gap I haven't seen.")),
  (t("Et le bois ?", "And the wood?"), t("Un chêne des vergers, une hache et un peu de dos : voilà comment on garde un village.", "An oak from the orchard, an axe and a strong back: that's how you keep a village safe.")),
  # The East Gate (sprint 129, docs/textes-v7.md §5.5): the fourth one, which she never counted.
  (t("La porte de l'Est ?", "The East Gate?"), t("Celle-là, je ne la comptais pas : une porte qui ne s'ouvre pas, c'est un mur. Ma grand-mère disait qu'on l'avait barrée « pour de bonnes raisons ». Elle n'a jamais dit lesquelles.", "That one I never counted: a gate that doesn't open is a wall. My grandmother said it was barred 'for good reasons'. She never said which."))],
 "firmin": [
  (None, t("Ah ! Tu m'as fait peur. Tout me fait peur, depuis les rats.", "Ah! You gave me a fright. Everything frightens me since the rats.")),
  (t("Et le moulin ?", "And the mill?"), t("Le moulin tourne avec l'étang. Sauf la nuit où la pierre s'est fendue : la roue a tourné à l'envers pendant une heure. Les enfants, sûrement.", "The mill runs on the pond. Except the night the stone cracked: the wheel turned backwards for a whole hour. The children, I'm sure.")),
  (t("Et l'étang ?", "And the pond?"), t("L'étang est plein de goujons. Ils ne mordent que pour les gens patients.", "The pond is full of gudgeon. They only bite for patient folk."))],
 "leonie": [
  (None, t("Bonjour ! Attention à Pétronille, c'est ma poule la plus curieuse.", "Hello! Mind Pétronille, she's my nosiest hen.")),
  (t("Un rêve ?", "A dream?"), t("J'ai rêvé d'un cercle qui reliait quatre lumières. Aubin et Garance aussi, mais eux n'en parlent pas. Moi, je trouvais ça joli.", "I dreamt of a circle joining four lights. So did Aubin and Garance, but they won't talk about it. I thought it was lovely.")),
  (t("Et le grenier ?", "And the loft?"), t("Le grenier garde mon grain et la farine de Firmin. Quand il n'y a pas de rats.", "The loft keeps my grain and Firmin's flour. When there aren't rats in it."))],
 "odalric": [
  (None, t("Bonjour. Parlez doucement, je vous prie : l'encre sèche.", "Good day. Speak softly, if you please: the ink is drying.")),
  (t("Le registre ?", "The registry?"), t("Le registre ne ment pas. Il lui arrive seulement de se taire.", "The registry does not lie. It merely falls silent at times.")),
  (t("Qu'y a-t-il dedans ?", "What is in it?"), t("Naissances, mariages, morts, récoltes : Clairval tient dans quatre gros volumes. Et dans le cimetière, bien sûr.", "Births, marriages, deaths, harvests: Clairval fits into four thick volumes. And into the churchyard, of course."))],
 "gaspard": [
  (None, t("Hein ? Parle plus fort, la roche m'a mangé les oreilles.", "Eh? Speak up, the rock ate my ears.")),
  (t("Le cuivre ?", "Copper?"), t("Le cuivre, c'est la roche verte, là. Approche-toi et frappe ; tout est dans ta fenêtre des métiers (J).", "Copper's the green rock, there. Walk up and strike it; it's all in your trades window (J).")),
  (t("Et la carrière ?", "And the quarry?"), t("Depuis peu, la carrière sonne quand on la frappe, comme une cloche au loin. Cinquante ans de mine, et je n'avais jamais entendu ça.", "Lately the quarry rings when you strike it, like a bell far away. Fifty years of mining and I'd never heard the like."))],
 "anselme": [
  (None, t("Hm. La forge est chaude. Parle vite.", "Hm. The forge is hot. Talk fast.")),
  (t("Le métier ?", "The trade?"), t("Du métal, du bois, de la patience. Le reste, c'est le marteau.", "Metal, wood, patience. The rest is the hammer.")),
  (t("L'arme de Garance ?", "Garance's weapon?"), t("Garance m'a apporté l'arme qui vibre. Le métal est bon. C'est la main qui ne l'est pas.", "Garance brought me the humming weapon. The metal is sound. It's the hand that isn't.")),
  # The forge (sprint 110, docs/textes-v6.md §2).
  (t("La forge ?", "The forge?"), t("Devant ma porte. Approche, ouvre ta fenêtre des métiers (J). Je ne prête pas mon marteau ; l'enclume, si.", "In front of my door. Step up and open your trades window (J). I don't lend my hammer; the anvil, yes."))],
 "perrine": [
  (None, t("Ne bouge pas, je prends tes mesures. Voilà. Tu peux bouger.", "Hold still, I'm taking your measurements. There. You may move.")),
  (t("Le métier ?", "The trade?"), t("Lin, cuir, peaux : je couds tout ce qui se coud, et droit. Le travers, je le laisse aux zombies.", "Flax, leather, pelts: I sew anything that can be sewn, and straight. Crooked I leave to the zombies.")),
  (t("Les peaux de loup ?", "Wolf pelts?"), t("Les peaux de loup qu'on m'apporte ont des poils raides comme de l'écorce. Je les couds quand même, mais ça m'agace.", "The wolf pelts I'm brought have hairs as stiff as bark. I sew them anyway, but it irks me.")),
  (t("L'atelier ?", "The workshop?"), t("Juste sous ma porte. Ouvre ta fenêtre des métiers (J), et ne touche pas à mes ciseaux : ils coupent droit, eux.", "Just below my door. Open your trades window (J), and don't touch my scissors: they cut straight, they do."))],
 # The jeweller and the cobbler of Craftsmen's Row (sprint 110, docs/textes-v6.md §2).
 "eudes": [
  (None, t("Approche, approche. Mes yeux ne voient plus bien loin, mais je n'oublie jamais un anneau.", "Come closer, closer. My eyes don't see very far any more, but I never forget a ring.")),
  (t("Le métier ?", "The trade?"), t("Des anneaux et des amulettes : le métal vient de la carrière, la colle des poissons de l'étang. Mon établi est sous ma porte ; ouvre ta fenêtre des métiers (J), et va doucement avec mes pinces.", "Rings and amulets: the metal comes from the quarry, the glue from the fish in the pond. My bench is just below my door; open your trades window (J), and go gently with my pliers.")),
  (t("Jamais ?", "Never?"), t("Chaque anneau que je fais, je l'inscris dans ce cahier, avec le doigt qui le porte. Les anneaux de cuivre que Garance donne à ses élèves, c'est moi. Ses insignes aussi.", "Every ring I make goes into this ledger, with the finger that wears it. The copper rings Garance gives her pupils are mine. Her badges too.")),
  (t("Rien d'étrange ?", "Nothing strange?"), t("Dans mon tiroir, il y a un anneau qui porte ma marque. Je me souviens de tous ceux que j'ai faits. Pas de celui-là.", "In my drawer there's a ring that bears my mark. I remember every one I've ever made. Not that one."))],
 "apolline": [
  (None, t("Mmf. Attends, j'ai des clous plein la bouche. Voilà. Montre tes pieds, je te dirai d'où tu viens.", "Mmf. Hang on, I've a mouthful of nails. There. Show me your feet and I'll tell you where you've been.")),
  (t("Le métier ?", "The trade?"), t("Bottes et ceintures, en cuir de poisson : Firmin me garde les peaux de ses goujons, il n'ose pas les vider lui-même. Mon banc est sous ma porte ; ta fenêtre des métiers (J) te dira le reste.", "Boots and belts, in fish leather: Firmin saves me the skins of his gudgeon, he daren't gut them himself. My bench is just below my door; your trades window (J) will tell you the rest.")),
  (t("Lire les pieds ?", "Reading feet?"), t("Firmin ? De la farine jusqu'aux chevilles. Mahaut ? Le talon gauche usé : elle pivote à gauche à chaque ronde. Les gens mentent, leurs semelles jamais.", "Firmin? Flour up to his ankles. Mahaut? Left heel worn down: she turns left on every round. People lie, their soles never do.")),
  (t("Rien d'étrange ?", "Nothing strange?"), t("Toutes les semelles de Clairval s'usent du côté de l'ouest. Toutes, même celles d'Eudes, qui ne sort jamais. Comme si le village entier penchait sans le savoir.", "Every sole in Clairval wears down on the west side. Every one, even Eudes's, and he never goes out. As if the whole village leant over without knowing it."))],
 "sidonie": [
  (None, t("Ne touche à rien ! Sauf à ça. Non, pas à ça non plus.", "Don't touch anything! Except that. No, not that either.")),
  (t("L'alchimie ?", "Alchemy?"), t("Une plante, de l'eau, du feu sous l'alambic et beaucoup de chance : c'est ça, l'alchimie. Surtout la chance.", "A plant, some water, a flame under the still and plenty of luck: that's alchemy. Mostly the luck.")),
  (t("Ces fioles ?", "These flasks?"), t("Les spores du Bois moisi restent côte à côte dans la fiole, sans jamais se mêler. Je n'avais jamais vu une poudre aussi bien élevée.", "Spores from the Mouldy Woods sit side by side in the flask and never mix. I've never seen such well-mannered powder."))],
 "basile": [
  (None, t("Mmh, bonjour. Je suis debout depuis trois heures, sois gentil.", "Mmh, morning. I've been up since three, be kind.")),
  (t("Le pain ?", "Bread?"), t("Des céréales, une herbe ou un poisson, et le four fait le reste. Le pain se mange hors combat : c'est lui qui te remet debout.", "Grain, a herb or a fish, and the oven does the rest. Bread is eaten outside fights: it's what puts you back on your feet.")),
  (t("La fête de la pierre ?", "The stone festival?"), t("Pour la fête de la pierre, on fait un pain à quatre entailles. Ma mère en faisait cinq, je crois. Ou quatre. Quatre, sûrement.", "For the stone festival we bake a loaf with four cuts. My mother made five, I think. Or four. Four, surely."))],
 "gedeon": [
  (None, t("Tu viens pour une clef ? Tout le monde vient pour une clef.", "Here for a key? Everyone's here for a key.")),
  (t("Une clef de donjon ?", "A dungeon key?"), t("Apporte les ressources, je te fais la clef d'un donjon. Une clef, une porte. Pas de crédit.", "Bring the materials and I'll make you a dungeon key. One key, one door. No credit.")),
  (t("Ces serrures ?", "Those locks?"), t("Les serrures des donjons sont plus vieilles que le village. Celui qui les a posées voulait qu'on n'entre pas. Moi, je veux qu'on ressorte.", "The dungeon locks are older than the village. Whoever fitted them wanted nobody to get in. I want folk to get out."))],
 "thibaut": [
  (None, t("Porte du Nord, chemin du Lac ! Enfin, le Bois d'abord, le Lac après.", "North Gate, the Lake Road! Well, the woods first, then the lake.")),
  (t("Rien à signaler ?", "Anything to report?"), t("La nuit, quand le vent vient du nord, on entend les grenouilles chanter. On dirait une berceuse. Mahaut dit que j'ai trop d'imagination.", "At night, when the wind's from the north, you can hear the frogs singing. Sounds like a lullaby. Mahaut says I've too much imagination."))],
 "mahaut": [
  (None, t("Halte. Ah, c'est toi. Passe, et reviens en un seul morceau.", "Halt. Oh, it's you. Go on, and come back in one piece.")),
  (t("Garance ?", "Garance?"), t("J'ai été l'élève de Garance, il y a dix ans. Elle crie toujours aussi fort ? Bien.", "I was Garance's pupil ten years ago. Does she still shout that loud? Good.")),
  (t("Rien à signaler ?", "Anything to report?"), t("Les sangliers grattent la palissade, la nuit. Ils n'avaient jamais fait ça.", "The boars scratch at the palisade at night. They never used to."))],
 # Charles wants horses in the stables (D75): Gautier's "there aren't any yet" became work horses, mounts later.
 "gautier": [
  (None, t("Porte du Sud, chemin des Champs. Et l'écurie, si tu cherches un cheval : ceux-là tirent la charrue, ils ne se montent pas. Pour les montures, il faudra attendre.", "South Gate, the Fields Road. And the stables, if you're after a horse: these ones pull the plough, they aren't for riding. Mounts will have to wait.")),
  (t("Rien à signaler ?", "Anything to report?"), t("Quand le vent vient des Champs, il sent le miel chaud. Le miel, ça ne devrait pas sentir chaud.", "When the wind blows from the fields, it smells of warm honey. Honey shouldn't smell warm."))],
 # The zones' people (sprint 105), each on a map by the way in from the woods (tools/zone_people.py).
 "mathurin": [
  (None, t("Doucement. Les arbres écoutent, et moi aussi.", "Easy. The trees are listening, and so am I.")),
  (t("Le métier ?", "The trade?"), t("On coupe ce qui doit l'être, on laisse ce qui doit rester. Le reste, c'est du bavardage.", "You cut what needs cutting and leave what needs to stay. The rest is chatter.")),
  (t("Et ce bois ?", "And these woods?"), t("Ce bois est plus vieux que Clairval. Il se souvient de choses que nous, on a laissé tomber.", "These woods are older than Clairval. They remember things we let fall."))],
 "barnabe": [
  (None, t("Chut. Ils dorment, les champignons. Toi aussi, tu devrais.", "Hush. They're sleeping, the mushrooms. You should too.")),
  (t("Ici, seul ?", "Here, alone?"), t("Quarante ans que je vis ici. Jamais ils ne m'ont mordu. Polis, ils sont.", "Forty years I've lived here. Never once have they bitten me. Polite, they are.")),
  (t("Le mycélium ?", "The mycelium?"), t("Le mycélium, il relie. Il ne mange pas, il ne mélange pas. Il tient la main, c'est tout.", "The mycelium, it links. It doesn't eat, it doesn't mix. It holds hands, that's all."))],
 "liane": [
  (None, t("Bonjour ! Attention aux planches, la troisième est pourrie.", "Hello! Mind the boards, the third one's rotten.")),
  (t("Le métier ?", "The trade?"), t("Je suis passeuse, comme celle d'avant moi. Tout le monde dit \"celle d'avant\", comme si elle n'avait pas de nom.", "I'm the ferrywoman, like the one before me. Everyone says 'the one before', as if she had no name.")),
  (t("Cet air ?", "That tune?"), t("(Elle fredonne.) Pardon, je chante sans m'en rendre compte. Tout le monde chante ça, ici.", "(She hums.) Sorry, I sing without noticing. Everyone sings that round here."))],
 # The old fisherman and his granddaughter on the lake's jetties (sprint 113, docs/textes-v5.md §3).
 "joachim": [
  (None, t("Hm. Le poisson ne mord pas, alors je parle. Qu'est-ce que tu veux ?", "Hm. The fish aren't biting, so I'm talking. What do you want?")),
  (t("Le lac ?", "The lake?"), t("Cinquante ans sur ce lac. Les grenouilles ne chantaient pas comme ça, avant.", "Fifty years on this lake. The frogs never sang like that before."))],
 "fleur": [
  (None, t("Tu sais chanter ? Moi oui. Les grenouilles m'ont appris.", "Can you sing? I can. The frogs taught me.")),
  (t("Ton grand-père ?", "Your grandad?"), t("Grand-père dit que les grenouilles ne savent pas chanter. Il a tort, mais faut pas lui dire.", "Grandad says frogs can't sing. He's wrong, but don't tell him."))],
 "eglantine": [
  (None, t("Ah, enfin quelqu'un ! Le vent tourne, la pluie arrive et mes ruches chauffent. Bonjour.", "Ah, someone at last! The wind's turning, the rain's coming and my hives are hot. Hello.")),
  (t("La ferme ?", "The farm?"), t("Du blé, des fleurs pour les abeilles, du miel pour Rose : la ferme, c'est trois métiers et pas de sommeil.", "Wheat, flowers for the bees, honey for Rose: a farm is three trades and no sleep.")),
  (t("Cette haie ?", "That hedge?"), t("Mon grand-père défendait d'arracher la vieille haie. Remarque, il défendait tout.", "My grandfather forbade anyone to pull up the old hedge. Mind you, he forbade everything."))],
 # The drill yard's second guard has no name nor lines in the scriptwriter's texts: one line of his own.
 "guard": [
  (None, t("Garance surveille la cour, moi la porte. Le Bois, lui, ne surveille personne.", "Garance watches the yard, I watch the gate. The woods watch nobody."))],
}
for npc, lines in P.items():
    ids = ["hello"] + [f"topic{k}" for k in range(1, len(lines))]
    def answers(skip):
        a = [O([("text", q), ("next", ids[k])]) for k, (q, _) in enumerate(lines) if q is not None and k != skip]
        return a + [O([("text", BYE)])]
    d = O([("id", npc), ("start", "hello"), ("lines", O((ids[k], O([("text", said), ("answers", answers(k))])) for k, (_, said) in enumerate(lines)))])
    open(f"{root}/dialogues/{npc}.json", "w").write(json.dumps(d, ensure_ascii=False, indent=2) + "\n")
print(len(P), "dialogues")
