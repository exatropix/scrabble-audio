# Scrabble audio

![Téléchargements](https://img.shields.io/github/downloads/exatropix/scrabble-audio/total?label=T%C3%A9l%C3%A9chargements)

**Un jeu de Scrabble contre l'ordinateur, entièrement au clavier, accessible avec NVDA et JAWS. Pour Windows 10 et 11. En français, en anglais et en espagnol.**

Par Exatropic.

## Le jeu

- Plateau classique de 15 sur 15 avec ses cases bonus, que l'on parcourt aux flèches : chaque case est annoncée, par exemple « H8, vide, mot compte double ».
- Le chevalet se manipule comme une zone de texte : on sélectionne son mot, on le copie, on le colle sur la case de départ, et le jeu vérifie le coup et compte les points avant de valider.
- L'ordinateur joue à trois niveaux : débutant, moyen, expert.
- Chaque langue a sa répartition officielle de jetons et ses points. En espagnol, CH, LL et RR sont des jetons à part entière.
- La partie est sauvegardée après chaque coup. Le jeu vérifie seul s'il existe une nouvelle version.

## Télécharger

1. Le jeu : [ScrabbleAudio.zip](https://github.com/exatropix/scrabble-audio/releases/latest/download/ScrabbleAudio.zip)
2. Un dictionnaire au moins :
   [français](https://github.com/exatropix/scrabble-audio/releases/latest/download/ScrabbleAudio-dictionnaire-fr.zip),
   [anglais](https://github.com/exatropix/scrabble-audio/releases/latest/download/ScrabbleAudio-dictionnaire-en.zip),
   [espagnol](https://github.com/exatropix/scrabble-audio/releases/latest/download/ScrabbleAudio-dictionnaire-es.zip)

Décompressez le jeu dans un dossier, puis le ou les dictionnaires dans ce même dossier. Lancez ScrabbleAudio.exe. Si Windows affiche « Windows a protégé votre ordinateur », choisissez « Informations complémentaires », puis « Exécuter quand même » : le programme n'est pas signé.

## Les commandes essentielles

- Ctrl+T : le chevalet. Maj+flèches pour sélectionner, Ctrl+C pour copier votre mot.
- Ctrl+P : le plateau. Flèches pour choisir la case, Ctrl+V pour poser, puis H ou V pour le sens. Entrée valide.
- Ctrl+R scores, Ctrl+S sac, Ctrl+M dernier coup, Ctrl+E échanger, Ctrl+J passer, Ctrl+N nouvelle partie, F1 aide complète.
- Menu Langue : la langue de l'interface et celle du jeu.

## English

A Scrabble game against the computer, fully playable from the keyboard with NVDA or JAWS, on Windows 10 and 11. French, English and Spanish, each with its official tile set. Download the game and at least one dictionary above, unzip them into the same folder and run ScrabbleAudio.exe. Switch to English in the Langue menu.

## Español

Un juego de Scrabble contra el ordenador, con el teclado, accesible con NVDA y JAWS, para Windows 10 y 11. En francés, inglés y español, cada uno con su reparto oficial de fichas (CH, LL, RR y Ñ en español). Descargue el juego y al menos un diccionario, descomprímalos en la misma carpeta y ejecute ScrabbleAudio.exe. Cambie al español en el menú Langue.

## Compiler

Le code source est dans le dossier `source` (C#, Windows Forms, .NET Framework 4.8). Par exemple :
`csc /target:winexe /out:ScrabbleAudio.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll source\App\*.cs source\Core\*.cs`
Placez à côté de l'exécutable le dossier sons et les fichiers nvdaControllerClient32.dll et nvdaControllerClient64.dll, fournis dans le zip du jeu.

## Licences

- Le code de ce projet est versé dans le domaine public (voir LICENSE).
- Dictionnaire français : Grammalecte / Dicollecte (MPL 2.0) et an-array-of-french-words (MIT).
- Dictionnaires anglais et espagnol : listes de mots de Letterpress, paquets word-list et an-array-of-spanish-words (MIT). Ce ne sont pas les listes officielles du Scrabble.
- Communication avec NVDA : nvdaControllerClient, de NV Access (LGPL, voir LICENSE-NVDA.txt).
