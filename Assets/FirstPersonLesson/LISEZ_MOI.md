# Cours : personnage en premiere personne, bras et objets

Ouvrir **Assets/DebugScene.unity**, puis appuyer sur Play et cliquer dans la vue Game.

| Commande | Action |
| --- | --- |
| Z / S | Avancer / reculer (clavier AZERTY) |
| Q / D | Se deplacer lateralement |
| Souris | Tourner la vue |
| E | Prendre la pioche ou le seau vise au centre de l'ecran, a moins de 3 metres |
| G | Deposer l'objet devant le personnage, si la place est libre |
| Echap | Liberer la souris et suspendre les commandes |
| Clic gauche | Reprendre le controle de la vue |

Les deux objets sont poses devant le point de depart. Baisser la vue pour les viser.
Un seul objet peut etre porte a la fois : prendre un autre objet depose automatiquement le precedent, qui tombe sous l'effet de la gravite.

## 1. Le deplacement

Lire dans cet ordre les scripts de `Assets/Character/Scripts/Player` :

1. `PlayerInputManager.cs` lit ZQSD et produit un Vector2. ClampMagnitude evite d'accelerer en diagonale.
2. `PlayerController.cs` transforme ce Vector2 en vitesse relative au corps. CharacterController.Move gere les collisions. La gravite est appliquee explicitement ; deltaTime rend la distance proportionnelle au temps.
3. `FirstPersonLook.cs` tourne le corps horizontalement et la camera verticalement. Le tangage est limite pour ne pas retourner la vue.

Exercice : changer moveSpeed dans l'Inspector. Comparer la distance parcourue en une seconde.
Pour etudier uniquement cette etape, desactiver PlayerInteractBehavior et l'objet enfant FirstPersonArms.

## 2. Les bras visibles

Reactiver FirstPersonArms sous la camera. Lire `Assets/FirstPersonLesson/FirstPersonArms.cs`.
Les avant-bras et mains sont extraits des maillages Body_Arms et Body_Hands du personnage HumanMale_Character_FREE du projet destination, puis poses dans le repere local de la camera. Leur materiau Body est celui du personnage humain existant. Ils restent donc visibles lorsque l'on tourne la tete.
La vitesse reelle du CharacterController commande un leger balancement sinusoidal. L'execution tardive permet de lire le deplacement de l'image courante, meme lorsqu'un mur bloque la marche. A l'arret, une interpolation ramene les bras au repos.

Les clips humanoides existants animent maintenant le squelette du personnage. FirstPersonArms reporte une amplitude reduite du mouvement des avant-bras animes sur les maillages visibles, en plus du leger balancement. HandAnchor suit le bras droit, donc l'objet porte suit aussi ce mouvement. Les maillages premiere personne ne possedent pas encore d'animation articulee independante des doigts.

Exercice : modifier amplitude et frequency sur FirstPersonArms, puis marcher et s'arreter.

## 3. Prendre et deposer un objet

Reactiver PlayerInteractBehavior. Lire `PlayerInteractBehavior.cs`, puis `Assets/FirstPersonLesson/PickupItem.cs`.

- Un rayon part du centre de la camera. Le premier obstacle decide ce qui peut etre vise ; un mur bloque le rayon.
- E appelle TryPickUp. Seuls les objets avec PickupItem peuvent etre pris. Si un objet est deja porte, il est depose avant de prendre le nouveau.
- PickupItem memorise l'etat physique, desactive ses colliders et passe son Rigidbody en mode kinematic.
- L'objet devient enfant de HandAnchor. Sa position et sa rotation locales definissent sa pose dans la main.
- G cherche une place libre devant le personnage, detache l'objet, reactive ses collisions et le rend dynamique avec gravite. L'objet peut tomber et etre repris.

Exercice : glisser `Prefabs/Bucket.prefab` dans la scene et tester cette seconde instance. Pour creer un autre objet ramassable : un maillage, un collider, un Rigidbody, puis PickupItem. Regler sa pose tenue dans l'Inspector.

Le seau et la pioche peuvent etre portes et deposes. Remplir le seau, miner, attaquer, sauvegarder ou jouer en reseau seront des fonctionnalites separees.

## Assets, origine et installation

Les prefabs reutilisables sont dans `Assets/FirstPersonLesson/Prefabs`.
`Source` contient SK_Player, SM_Pickaxe_Tier1, SM_Bucket_Tier1 et TEX_GradientPalette, extraits de `RecoveredBinary/ExportedProject/Assets` du projet source. SK_Player et SourceBoneNames.json sont des references historiques de la premiere version des bras et ne sont plus utilises par les bras visibles. Les outils utilisent le shader Standard et la palette source ; les bras utilisent le materiau Body du personnage humain destination.

Les concepts sont adaptes des scripts sources PlayerCharacter / PlayerCamera et PlayerItemHolder / HeldItem. Le controleur destination existant est conserve et commente ; les mecanismes reseau et les dependances du jeu complet ne sont pas importes.

`Editor/FirstPersonLessonSetup.cs` montre comment les prefabs sont generes avec les API Unity. `Editor/HumanArmMeshBuilder.cs` extrait les bras et les mains du personnage humain. Le menu School > Replace first person arms with human arms permet de regenerer ces maillages. Le menu **School > Install first person lesson in current scene** permet d'installer le meme cours dans une autre scene possedant un PlayerController et sa camera. Il ne faut pas l'executer a nouveau dans DebugScene : le cours y est deja installe. Le prefab des bras seul ne raccorde pas l'interaction ; le menu effectue aussi cette liaison.

Le marqueur historique Interactible, deja attache a des objets, etait declare comme interface et provoquait une erreur ExtensionOfNativeClass. Il herite maintenant de MonoBehaviour. Les nouveaux objets ramassables utilisent PickupItem.

La scene adaptee est **DebugScene**. L'autre scene `Scene.unity` n'a pas ete migree.

## Verification

Scripts compiles et controles en Play dans Unity 6000.6.7f1 : avance, deplacement lateral, collision avec un mur, contact au sol, balancement des bras, ciblage des deux outils, prise, etat tenu, refus d'une seconde prise, depot, restitution de la physique, et blocage de prise/depot par un mur. Inspection visuelle de la vue premiere personne et de la pioche tenue.

Les controles de deplacement ont fourni les directions au meme controleur que le clavier ; les touches physiques restent a essayer par l'eleve dans Game. Aucun executable autonome n'a ete construit.

Les sauvegardes des scripts et de DebugScene avant intervention sont dans `FeatureTransfer/Before` du projet source. Les scripts de verification et captures y sont egalement conserves dans FeatureTransfer.



## 4. Les animations de deplacement

Le personnage utilise maintenant `Assets/FirstPersonLesson/PlayerLocomotion.controller`. L'ancien controleur reste disponible dans Character/Animators.

Les clips existants sont reutilises : Idle/Idle.anim, Locomotion/walk_fwd.anim et Locomotion/strafe.anim. Un Blend Tree 2D melange les poses selon MoveX (gauche/droite) et MoveY (avant/arriere). Au centre : repos. Le recul utilise la marche a l'envers. Le clip strafe fourni va a gauche ; il est miroir pour aller a droite. Les diagonales melangent les clips voisins.

Lire `PlayerLocomotionAnimator.cs` : apres le deplacement, la vitesse reelle du CharacterController est convertie dans le repere du joueur, puis envoyee aux deux parametres avec un lissage. Un mur qui bloque le joueur reduit donc aussi son animation. Walking Speed correspond a la vitesse de deplacement de reference (4 metres/seconde ici).

Root Motion est desactive : les clips animent les os, tandis que PlayerController gere toujours les deplacements et collisions. Always Animate permet au squelette de continuer a animer les bras en premiere personne meme si le corps est hors champ.

`Editor/LocomotionLessonSetup.cs` documente la creation du Blend Tree. Le menu School > Install player locomotion animations permet de raccorder ce controleur a un joueur dans une autre scene. DebugScene est deja configuree.

Exercice : ouvrir le controleur dans Animator pendant Play, marcher avec ZQSD, puis observer MoveX/MoveY et les poids des clips. S'arreter et observer le retour progressif a Idle.

## Mise a jour : bras du personnage et pose de portage

Les deux maillages supplementaires LeftArm et RightArm ont ete supprimes de DebugScene et du prefab FirstPersonArms. Le composant FirstPersonArms conserve seulement le balancement sinusoidal de la cible de prise.
PlayerHoldingAnimator anime le bras droit du squelette humanoide existant avec OnAnimatorIK. Une transition de 0,18 seconde applique la pose quand un objet est tenu et la retire au depot. La cible commune a la main et a l'objet est limitee a la longueur reelle du bras. Le point de vue est calibre sur le squelette et les maillages de tete ne cachent plus la vue en premiere personne pendant Play.
Le balancement de marche est conserve, ainsi que le controleur de locomotion. La passe IK du premier calque Animator est activee. Aucun nouveau maillage de bras n'est ajoute.
Verification en Play : prise et depot reussis, poids de pose de 1 a 0, ecart main/cible inferieur a 0,001 metre, cible devant la camera et balancement mesure pendant le deplacement. La scene est rendue a l'etat Edition apres verification.
Les sauvegardes avant modification sont dans FeatureTransfer/BeforeHoldingPose du projet School Project. Le menu School > Use original arms and animate holding documente l'installation. L'ancien menu de generation de bras correspond a la version precedente et ne doit pas etre relance pour cette configuration.

### Correction du visage et de la camera
Le code qui rendait les maillages de tete invisibles (ShadowsOnly) a ete retire. Le visage reste rendu normalement. La camera est placee devant la tete (decalage avant de 0,22 m depuis le repere de tete initial ; environ 0,15 m devant la tete animee dans le test), avec un plan proche de 0,03 m. Cette correction remplace la note precedente sur le masquage du visage.
Verification en Play : aucun SkinnedMeshRenderer en ShadowsOnly, camera devant la tete, absence de geometrie du visage dans le rendu premiere personne, prise/depot et balancement conserves.
