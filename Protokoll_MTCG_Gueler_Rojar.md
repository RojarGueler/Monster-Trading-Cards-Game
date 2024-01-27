# Protocol Monster Trading Card Game - Güler Rojar

#### Describe lessons learned: 

Das ist das erste Mal, dass ich mit C# gearbeitet habe. Dadurch habe ich einiges über diese Programmiersprache gelernt. Im Endeffekt ist sie zwar ähnlich zu Java, aber es gibt hier und da doch Unterschiede. Außerdem konnte ich mein Wissen in SQL auffrischen. Zusätzlich habe ich wieder mit Unit Tests gearbeitet, mit denen ich schon länger nichts mehr zu tun hatte.

#### Design:

Für mein Projekt habe ich kein spezifisches Pattern benutzt. Ich habe die Basic Funktionen aufgeteilt (Request, Response, Datenbank,...) und somit eine Struktur erschaffen, mit der ich gut arbeiten konnte. Es war wahrscheinlich nicht die effizienteste Methode, um mit so einem Projekt zu starten, hat aber trotzdem ausgereicht.  
Ich habe öfters im Projekt meine Struktur in den Klassen geändert, damit sie übersichtlicher sind. Zum Beispiel habe ich Handler Methoden in der Request Klasse geaddet, anstatt alles in einer Methode zu handeln.

#### Describe Unique Feature:

##### Wheel of Fortune (Glücksrad):

Einmal am Tag kann ein User, der angemeldet/registriert ist, an einem Glücksrad drehen, bei dem er zwischen 1-10 Coins gewinnen kann. Wenn der selbe User versucht mehrmals am Rad zu drehen kommt ein Error.

Dafür muss er diese curl Befehle eingeben:
GET .../coins <-- damit man die coins des Users liest

PUT .../coins <-- fügt coins per Glücksrad dem User hinzu 

#### Describe Unit Tests:

In den Unit Tests teste ich verschiedene Methoden, wie zum Beispiel den User login. Ich habe die Unit Tests nicht im Voraus geschrieben, sondern erst am ende. Klüger wäre die Herangehensweise des Test-Driven-Development, aber da ich mit der Entwicklung des Projektes angefangen hatte und erst danach gesehen habe, dass man Unit Tests braucht, war es das auch somit damit. Es wurden insgesamt 22 Unit Tests geschrieben.

#### Tracked Time:

Ich habe insgesamt etwa 85 Stunden an dem Projekt gearbeitet.

#### Link to Git:

Ich hatte leider vergessen anfangs zu commiten und zu pushen, weil ich das Repository noch nicht aufgesetzt hatte. Deshalb erst ab Punkt 14. 

https://github.com/RojarGueler/Monster-Trading-Cards-Game