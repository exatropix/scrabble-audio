using ScrabbleAudio.Core;

namespace ScrabbleAudio
{
    static class Aide
    {
        const string TexteFr =
@"SCRABBLE AUDIO, VERSION 2.0

Ce jeu se joue entièrement au clavier. Tout ce qu'il annonce passe par votre lecteur d'écran, NVDA ou JAWS, avec votre voix habituelle. Sans lecteur d'écran, la voix de Windows prend le relais.
Appuyez sur Échap pour fermer cette aide.

LES COMMANDES

Ctrl+T : aller sur votre chevalet.
Ctrl+P : aller sur le plateau.
Ctrl+R : résultat, c'est-à-dire les scores des deux joueurs.
Ctrl+S : nombre de lettres restant dans le sac.
Ctrl+M : mémoire, rappelle le dernier coup joué. Appuyez de nouveau pour remonter dans l'historique.
Ctrl+E : échanger des lettres.
Ctrl+J : passer son tour.
Ctrl+N : nouvelle partie.
Ctrl+Q : arrêter le jeu, avec proposition de sauvegarder.
F1 : cette aide.
Alt : ouvrir le menu, qui reprend toutes les actions.

LES MISES À JOUR

Au démarrage, une fois par jour, le jeu vérifie sur Internet si une nouvelle version existe et vous prévient, sans rien installer. Dans le menu Aide, Rechercher une mise à jour propose d'ouvrir la page de téléchargement ; une case permet de désactiver la vérification au démarrage. Vos parties sauvegardées sont conservées.

LES LANGUES

Le menu Langue propose deux réglages :
- Interface : la langue des menus, des annonces et de cette aide. Français, anglais ou espagnol.
- Langue du jeu : la langue des mots, avec sa propre répartition de lettres et ses propres points. Elle s'applique à la partie suivante.
En espagnol, CH, LL et RR sont des jetons à part entière, comme dans le Scrabble espagnol officiel, et le Ñ est une lettre. Il n'y a pas de jeton K ni W : seul un joker peut les jouer. Pour un joker, tapez CH, LL, RR ou Ñ quand le jeu demande la lettre.

SUR LE CHEVALET

Le chevalet se manipule comme une zone de texte de Windows.
Flèche gauche et flèche droite : passer d'une lettre à l'autre. Chaque lettre est annoncée avec sa valeur, par exemple : A, 1.
Début et Fin : aller au début ou à la fin du chevalet.
Maj+flèches : sélectionner des lettres. Ctrl+A : tout sélectionner. Échap : effacer la sélection.
Ctrl+C : copier les lettres sélectionnées. C'est votre mot, ou une partie du mot. Les lettres restent sur le chevalet : elles ne le quittent qu'une fois posées et validées sur le plateau.
Ensuite, allez sur le plateau avec Ctrl+P.
Pour réorganiser vos lettres, si vous le souhaitez : Ctrl+X coupe les lettres sélectionnées, Ctrl+V les colle à l'endroit du curseur.

SUR LE PLATEAU

Les lignes sont nommées de A à O, de haut en bas. Les colonnes sont numérotées de 1 à 15, de gauche à droite. La case centrale est H8.
Flèches : se déplacer d'une case. Chaque case est annoncée, par exemple : H8, vide, mot compte double, ou : H9, A.
Ctrl+flèche : sauter jusqu'à la prochaine lettre posée dans cette direction.
L : lire toute la ligne. C : lire toute la colonne.
Placez-vous sur la case de départ, puis Ctrl+V pour poser les lettres copiées. Un menu demande le sens : Horizontal ou Vertical. Les touches H et V choisissent directement.
Le jeu place vos lettres en sautant les cases déjà occupées. Par exemple, pour écrire MAISON en croisant un A déjà posé, sélectionnez M, I, S, O, N sur le chevalet, copiez-les, placez-vous sur la case du M, et collez.
Si vous collez un joker, le jeu vous demande quelle lettre il remplace.
Si la pose est impossible, le jeu explique pourquoi et vous pouvez chercher un autre endroit.
Si elle est possible, le jeu annonce le mot et les points. Avant de valider, vous pouvez vérifier : Espace épelle le mot lettre par lettre, et les flèches parcourent le plateau, les lettres en attente étant annoncées « posée ». Entrée valide, Échap annule.
Toutes les annonces passent par votre lecteur d'écran, avec votre voix habituelle.

DÉROULEMENT D'UNE PARTIE

Au lancement, si une partie est sauvegardée, le jeu propose de la reprendre. Sinon, une nouvelle partie commence contre l'ordinateur, et vous choisissez son niveau : débutant, moyen ou expert.
Le premier joueur est tiré au sort. Chacun reçoit 7 lettres.
Après chaque coup, le joueur complète son chevalet à 7 lettres en piochant dans le sac.
Le premier mot doit passer par la case centrale H8. Ensuite, chaque mot doit toucher une lettre déjà posée.
Tous les mots formés, le mot principal comme ceux formés au passage, doivent exister dans le dictionnaire.
La partie se termine quand le sac est vide et qu'un joueur a posé toutes ses lettres, ou après 6 tours de suite sans point (échanges ou passes).

LES POINTS

Valeur des lettres dans la langue du jeu : {BAREME}
Une case lettre compte double ou triple multiplie la valeur de la lettre posée dessus.
Une case mot compte double ou triple multiplie la valeur de tout le mot.
Les cases bonus ne comptent que pour les lettres que l'on vient de poser.
Poser ses 7 lettres d'un coup s'appelle un scrabble et rapporte une prime de 50 points.
En fin de partie, chacun perd la valeur des lettres qui lui restent. Le joueur qui a tout posé gagne la valeur des lettres restantes de son adversaire.

LE DICTIONNAIRE

Chaque langue a son dictionnaire, qui se télécharge séparément sur la page du jeu. Décompressez-le dans le dossier du jeu : il se range dans le dossier dictionnaires, sous-dossier fr, en ou es.
Ce sont des listes de mots libres, qui ne sont pas les listes officielles du Scrabble. Quelques mots peuvent manquer ou être en trop.
Chaque liste est le fichier mots.txt, un mot par ligne, que vous pouvez compléter.

LES SONS

Les sons se trouvent dans le dossier sons. Vous pouvez remplacer chaque fichier par le vôtre, en gardant le même nom et le format WAV : distribution, pose, erreur, ordinateur, fin, bord, selection.
";

        const string TexteEn =
@"SCRABBLE AUDIO, VERSION 2.0

This game is played entirely from the keyboard. Everything it says goes through your screen reader, NVDA or JAWS, with your usual voice. Without a screen reader, the Windows voice takes over.
Press Escape to close this help.

THE COMMANDS

Ctrl+T: go to your rack.
Ctrl+P: go to the board.
Ctrl+R: result, that is the scores of both players.
Ctrl+S: number of letters left in the bag.
Ctrl+M: memory, repeats the last move played. Press again to go back through the history.
Ctrl+E: exchange letters.
Ctrl+J: pass your turn.
Ctrl+N: new game.
Ctrl+Q: quit the game, with an offer to save.
F1: this help.
Alt: open the menu, which contains all the actions.

UPDATES

At startup, once a day, the game checks online whether a new version exists and tells you, without installing anything. In the Help menu, Check for updates offers to open the download page; a checkbox turns off the check at startup. Your saved games are kept.

LANGUAGES

The Language menu has two settings:
- Interface: the language of the menus, announcements and this help. French, English or Spanish.
- Game language: the language of the words, with its own letter distribution and points. It applies from the next game.
In Spanish, CH, LL and RR are tiles in their own right, as in the official Spanish Scrabble, and Ñ is a letter. There is no K or W tile: only a blank can play them. For a blank, type CH, LL, RR or Ñ when the game asks for the letter.

ON THE RACK

The rack works like a Windows text box.
Left and right arrows: move from one letter to the next. Each letter is announced with its value, for example: A, 1.
Home and End: go to the start or the end of the rack.
Shift+arrows: select letters. Ctrl+A: select all. Escape: clear the selection.
Ctrl+C: copy the selected letters. This is your word, or part of it. The letters stay on the rack: they only leave it once placed and confirmed on the board.
Then go to the board with Ctrl+P.
To rearrange your letters, if you wish: Ctrl+X cuts the selected letters, Ctrl+V pastes them at the cursor.

ON THE BOARD

Rows are named A to O, from top to bottom. Columns are numbered 1 to 15, from left to right. The centre square is H8.
Arrows: move one square. Each square is announced, for example: H8, empty, double word score, or: H9, A.
Ctrl+arrow: jump to the next placed letter in that direction.
L: read the whole row. C: read the whole column.
Go to the starting square, then Ctrl+V to place the copied letters. A menu asks for the direction: Horizontal or Vertical. The H and V keys choose directly.
The game places your letters, skipping squares that are already taken. For example, to write HOUSE across an O already on the board, select H, U, S, E on the rack, copy them, go to the square of the H, and paste.
If you paste a blank, the game asks which letter it stands for.
If the move is not possible, the game explains why and you can look for another place.
If it is possible, the game announces the word and the points. Before confirming, you can check: Space spells the word letter by letter, and the arrows move around the board, the pending letters being announced as placed. Enter confirms, Escape cancels.

HOW A GAME GOES

When it starts, if a game is saved, the game offers to resume it. Otherwise, a new game starts against the computer, and you choose its level: beginner, intermediate or expert.
The first player is drawn at random. Each player receives 7 letters.
After each move, the player refills the rack to 7 letters from the bag.
The first word must cover the centre square H8. After that, each word must touch a letter already on the board.
All the words formed, the main word and those formed along the way, must be in the dictionary.
The game ends when the bag is empty and a player has placed all their letters, or after 6 turns in a row without points (exchanges or passes).

POINTS

Letter values in the game language: {BAREME}
A double or triple letter square multiplies the value of the letter placed on it.
A double or triple word square multiplies the value of the whole word.
Bonus squares only count for the letters just placed.
Placing all 7 letters at once is called a bingo and earns a 50-point bonus.
At the end of the game, each player loses the value of the letters left on their rack. The player who went out gains the value of the opponent's remaining letters.

THE DICTIONARY

Each language has its own dictionary, downloaded separately from the game page. Unzip it into the game folder: it goes into the dictionnaires folder, in the fr, en or es subfolder.
These are free word lists, not the official Scrabble lists. A few words may be missing or extra.
Each list is the file mots.txt, one word per line, which you can add to.

SOUNDS

The sounds are in the sons folder. You can replace each file with your own, keeping the same name and the WAV format: distribution, pose, erreur, ordinateur, fin, bord, selection.
";

        const string TexteEs =
@"SCRABBLE AUDIO, VERSIÓN 2.0

Este juego se juega por completo con el teclado. Todo lo que dice pasa por su lector de pantalla, NVDA o JAWS, con su voz habitual. Sin lector de pantalla, toma el relevo la voz de Windows.
Pulse Escape para cerrar esta ayuda.

LAS ÓRDENES

Ctrl+T: ir a su atril.
Ctrl+P: ir al tablero.
Ctrl+R: resultado, es decir, las puntuaciones de los dos jugadores.
Ctrl+S: número de fichas que quedan en la bolsa.
Ctrl+M: memoria, repite la última jugada. Púlselo de nuevo para retroceder en el historial.
Ctrl+E: cambiar fichas.
Ctrl+J: pasar el turno.
Ctrl+N: nueva partida.
Ctrl+Q: salir del juego, con la opción de guardar.
F1: esta ayuda.
Alt: abrir el menú, que contiene todas las acciones.

LAS ACTUALIZACIONES

Al iniciarse, una vez al día, el juego comprueba en Internet si existe una nueva versión y le avisa, sin instalar nada. En el menú Ayuda, Buscar actualizaciones propone abrir la página de descarga; una casilla desactiva la comprobación al iniciar. Sus partidas guardadas se conservan.

LOS IDIOMAS

El menú Idioma tiene dos ajustes:
- Interfaz: el idioma de los menús, de los anuncios y de esta ayuda. Francés, inglés o español.
- Idioma del juego: el idioma de las palabras, con su propio reparto de fichas y sus propios puntos. Se aplica a partir de la siguiente partida.
En español, CH, LL y RR son fichas propias, como en el Scrabble español oficial, y la Ñ es una letra. No hay ficha K ni W: solo un comodín puede jugarlas. Para un comodín, escriba CH, LL, RR o Ñ cuando el juego pida la letra.

EN EL ATRIL

El atril funciona como un cuadro de texto de Windows.
Flechas izquierda y derecha: pasar de una ficha a otra. Cada ficha se anuncia con su valor, por ejemplo: A, 1.
Inicio y Fin: ir al principio o al final del atril.
Mayúsculas+flechas: seleccionar fichas. Ctrl+A: seleccionar todo. Escape: borrar la selección.
Ctrl+C: copiar las fichas seleccionadas. Es su palabra, o una parte. Las fichas se quedan en el atril: solo lo dejan cuando se colocan y se confirman en el tablero.
Después, vaya al tablero con Ctrl+P.
Para reordenar sus fichas, si lo desea: Ctrl+X corta las fichas seleccionadas, Ctrl+V las pega en el cursor.

EN EL TABLERO

Las filas se llaman de la A a la O, de arriba abajo. Las columnas van del 1 al 15, de izquierda a derecha. La casilla central es H8.
Flechas: moverse una casilla. Cada casilla se anuncia, por ejemplo: H8, vacía, palabra doble, o: H9, A.
Ctrl+flecha: saltar hasta la siguiente ficha colocada en esa dirección.
L: leer toda la fila. C: leer toda la columna.
Colóquese en la casilla de partida y pulse Ctrl+V para colocar las fichas copiadas. Un menú pregunta la dirección: Horizontal o Vertical. Las teclas H y V eligen directamente.
El juego coloca sus fichas saltando las casillas ya ocupadas. Por ejemplo, para escribir CASA cruzando una A ya colocada, seleccione C, S, A en el atril, cópielas, colóquese en la casilla de la C y pegue.
Si pega un comodín, el juego le pregunta qué letra representa.
Si la jugada no es posible, el juego explica por qué y puede buscar otro sitio.
Si es posible, el juego anuncia la palabra y los puntos. Antes de confirmar, puede comprobar: Espacio deletrea la palabra, y las flechas recorren el tablero, anunciando las fichas pendientes como colocadas. Intro confirma, Escape anula.

DESARROLLO DE UNA PARTIDA

Al abrirse, si hay una partida guardada, el juego propone reanudarla. Si no, empieza una nueva partida contra el ordenador, y usted elige su nivel: principiante, medio o experto.
El primer jugador se sortea. Cada uno recibe 7 fichas.
Después de cada jugada, el jugador completa su atril hasta 7 fichas robando de la bolsa.
La primera palabra debe pasar por la casilla central H8. Después, cada palabra debe tocar una ficha ya colocada.
Todas las palabras formadas, la principal y las que se forman de paso, deben estar en el diccionario.
La partida termina cuando la bolsa está vacía y un jugador ha colocado todas sus fichas, o tras 6 turnos seguidos sin puntos (cambios o pases).

LOS PUNTOS

Valor de las fichas en el idioma del juego: {BAREME}
Una casilla de letra doble o triple multiplica el valor de la ficha colocada en ella.
Una casilla de palabra doble o triple multiplica el valor de toda la palabra.
Las casillas de premio solo cuentan para las fichas que se acaban de colocar.
Colocar las 7 fichas de una vez se llama scrabble y da un premio de 50 puntos.
Al final de la partida, cada uno pierde el valor de las fichas que le quedan. El jugador que lo ha colocado todo gana el valor de las fichas que le quedan a su adversario.

EL DICCIONARIO

Cada idioma tiene su diccionario, que se descarga por separado en la página del juego. Descomprímalo en la carpeta del juego: se coloca en la carpeta dictionnaires, subcarpeta fr, en o es.
Son listas de palabras libres, no las listas oficiales del Scrabble. Puede faltar o sobrar alguna palabra.
Cada lista es el archivo mots.txt, una palabra por línea, que puede completar.

LOS SONIDOS

Los sonidos están en la carpeta sons. Puede sustituir cada archivo por el suyo, conservando el mismo nombre y el formato WAV: distribution, pose, erreur, ordinateur, fin, bord, selection.
";

        public static string Texte
        {
            get
            {
                string t = L.Code == "en" ? TexteEn : L.Code == "es" ? TexteEs : TexteFr;
                return t.Replace("{BAREME}", Lettres.Bareme());
            }
        }
    }
}
