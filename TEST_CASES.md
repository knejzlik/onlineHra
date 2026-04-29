# Test Cases - Online MUD Game

Tento dokument obsahuje testovací případy pro online MUD hru. Testy jsou rozděleny mezi členy týmu S a L rovnoměrně podle odpovědností definovaných v návrhovém dokumentu.

---

## Rozdělení testů

### Člověk S (Server & Backend)
- Unit testy pro modely (PlayerState, Room, Item, Npc)
- Unit testy pro služby (WorldService, PlayerService)
- Unit testy pro příkazy: `take`, `drop`, `attack`, `trade`, `give`
- Integrace testy pro serverovou logiku
- Testy persistencie dat

### Člověk L (Klient & Komunikace)
- Unit testy pro příkazy: `go`, `inventory`, `help`, `explore`, `say`, `whisper`, `broadcast`, `talk`
- Testy komunikačního protokolu
- Testy síťové komunikace klient-server
- End-to-end testy herního cyklu
- Testy více hráčů současně

---

## Test Case 1: Registrace a přihlášení hráče

**ID:** TC001  
**Priorita:** Vysoká  
**Přiřazeno:** S  
**Komponenta:** PlayerService  

**Popis:** Ověření funkčnosti registrace nového hráče a následného přihlášení.

**Pre-kondice:**
- Server běží na portu 65525
- Data složka existuje s prázdným nebo neexistujícím players.json

**Testovací kroky:**
1. Připojit klienta k serveru
2. Poslat příkaz: `register testuser testpassword123`
3. Ověřit odpověď obsahuje "Registration successful"
4. Odpojit klienta
5. Připojit nového klienta
6. Poslat příkaz: `login testuser testpassword123`
7. Ověřit odpověď obsahuje "Login successful"

**Očekávaný výsledek:**
- Registrace proběhne úspěšně
- Hráč je uložen v players.json
- Přihlášení se stejnými údaji uspěje
- Nesprávné heslo vrátí chybu

**Testovací data:**
```
Username: testuser
Password: testpassword123
```

---

## Test Case 2: Pohyb mezi místnostmi

**ID:** TC002  
**Priorita:** Vysoká  
**Přiřazeno:** L  
**Komponenta:** GoCommand, WorldService  

**Popis:** Ověření pohybu hráče mezi propojenými místnostmi.

**Pre-kondice:**
- Hráč je přihlášen a nachází se v místnosti "start"
- Místnosti jsou načteny z rooms.json

**Testovací kroky:**
1. Poslat příkaz: `go north`
2. Ověřit odpověď obsahuje "You go north" a "Main Hallway"
3. Poslat příkaz: `go west`
4. Ovéřit odpověď obsahuje "Armory"
5. Poslat příkaz: `go east`
6. Ověřit návrat do "Main Hallway"
7. Poslat příkaz: `go south`
8. Ověřit návrat do "Cave Entrance"

**Očekávaný výsledek:**
- Hráč se přesune do správné místnosti
- Popis místnosti se zobrazí po každém pohybu
- Exit direction je validní

**Testovací data:**
```
Start room: start
Path: start → hallway → armory → hallway → start
```

---

## Test Case 3: Vzetí předmětu do inventáře

**ID:** TC003  
**Priorita:** Vysoká  
**Přiřazeno:** S  
**Komponenta:** TakeCommand, PlayerService  

**Popis:** Ověření funkčnosti příkazu take pro sběr předmětů.

**Pre-kondice:**
- Hráč je v místnosti "armory"
- V místnosti jsou předměty: sharp_sword, rusty_key

**Testovací kroky:**
1. Poslat příkaz: `take sword`
2. Ověřit odpověď obsahuje "You picked up the Sharp Sword"
3. Poslat příkaz: `inventory`
4. Ověřit že "Sharp Sword" je v inventáři
5. Poslat příkaz: `take key`
6. Ověřit že "Rusty Key" byl přidán do inventáře

**Očekávaný výsledek:**
- Předmět je odstraněn z místnosti
- Předmět je přidán do inventáře hráče
- Inventář ukazuje správnou váhu

**Testovací data:**
```
Room: armory
Items to take: sharp_sword, rusty_key
Max capacity: 10
```

---

## Test Case 4: Odhození předmětu

**ID:** TC004  
**Priorita:** Střední  
**Přiřazeno:** S  
**Komponenta:** DropCommand  

**Popis:** Ověření funkčnosti příkazu drop pro odložení předmětů.

**Pre-kondice:**
- Hráč má v inventáři "rusty_key"
- Hráč je v místnosti "hallway"

**Testovací kroky:**
1. Poslat příkaz: `drop key`
2. Ověřit odpověď obsahuje "You dropped the Rusty Key"
3. Poslat příkaz: `inventory`
4. Ověřit že klíč není v inventáři
5. Poslat příkaz: `explore`
6. Ověřit že klíč je v seznamu předmětů v místnosti

**Očekávaný výsledek:**
- Předmět je odebrán z inventáře
- Předmět je přidán do místnosti
- Jiní hráči v místnosti vidí odhozený předmět

---

## Test Case 5: Boj s NPC

**ID:** TC005  
**Priorita:** Vysoká  
**Přiřazeno:** S  
**Komponenta:** AttackCommand  

**Popis:** Ověření bojového systému s NPC postavami.

**Pre-kondice:**
- Hráč má v inventáři "sharp_sword"
- Hráč je v místnosti "tower"
- V místnosti je NPC "boss_guard" s HP=10

**Testovací kroky:**
1. Poslat příkaz: `attack knight`
2. Ověřit odpověď obsahuje "strikes down Dark Knight"
3. Ověřit že z NPC dropnul "dragon_scale"
4. Poslat příkaz: `take scale`
5. Ověřit že hráč získal Dragon Scale
6. Zkusit znovu zaútočit na stejné NPC
7. Ověřit odpověď že NPC je již mrtvé

**Očekávaný výsledek:**
- NPC je poraženo jedním úderem (pro jednoduchost)
- Z NPC dropne předmět
- NPC je odstraněno z místnosti
- Nelze znovu zaútočit na mrtvé NPC

**Testovací data:**
```
NPC: boss_guard
Required weapon: sharp_sword
Drop item: dragon_scale
```

---

## Test Case 6: Obchod s drakem

**ID:** TC006  
**Priorita:** Vysoká  
**Přiřazeno:** S  
**Komponenta:** TradeCommand  

**Popis:** Ověření mechaniky obchodu s NPC drakem.

**Pre-kondice:**
- Hráč má v inventáři "fake_egg"
- Hráč je v místnosti "lair" s drakem

**Testovací kroky:**
1. Poslat příkaz: `trade egg`
2. Ověřit odpověď obsahuje "successfully traded"
3. Poslat příkaz: `inventory`
4. Ověřit že fake_egg byl vyměněn za golden_egg
5. Zkusit obchodovat s jiným předmětem
6. Ověřit že drak nemá zájem

**Očekávaný výsledek:**
- Fake egg je vyměněn za golden egg
- Jiné předměty nelze obchodovat
- Transakce je uložena

**Testovací data:**
```
Give: fake_egg
Receive: golden_egg
NPC: dragon
```

---

## Test Case 7: Komunikace mezi hráči

**ID:** TC007  
**Priorita:** Střední  
**Přiřazeno:** L  
**Komponenta:** SayCommand, WhisperCommand, BroadcastCommand  

**Popis:** Ověření různých typů komunikace mezi hráči.

**Pre-kondice:**
- Dva hráči (PlayerA, PlayerB) jsou připojeni
- Oba jsou ve stejné místnosti "hallway"

**Testovací kroky:**
1. PlayerA pošle: `say Hello everyone!`
2. Ověřit že PlayerB vidí "[PlayerA] says: \"Hello everyone!\""
3. PlayerA pošle: `whisper PlayerB Secret message`
4. Ověřit že jen PlayerB vidí zprávu
5. PlayerA pošle: `broadcast Attention all players!`
6. Ověřit že všichni hráči na serveru vidí zprávu

**Očekávaný výsledek:**
- Say je vidět pouze hráčům ve stejné místnosti
- Whisper je vidět pouze cílovému hráči
- Broadcast je vidět všem připojeným hráčům

---

## Test Case 8: Interakce s NPC (Talk)

**ID:** TC008  
**Priorita:** Střední  
**Přiřazeno:** L  
**Komponenta:** TalkCommand  

**Popis:** Ověření dialogového systému s NPC.

**Pre-kondice:**
- Hráč je v místnosti "start"
- V místnosti je NPC "guide" (Wounded Soldier)

**Testovací kroky:**
1. Poslat příkaz: `talk soldier`
2. Ověřit odpověď s default dialogem o armory
3. Poslat příkaz: `talk soldier about dragon`
4. Ověřit odpověď s informací o drakovi
5. Poslat příkaz: `talk nonexistent`
6. Ověřit chybovou hlášku

**Očekávaný výsledek:**
- NPC odpoví podle triggeru v dialogs
- Default odpověď při neznámém triggeru
- Chyba při neexistujícím NPC

**Testovací data:**
```
NPC: guide
Triggers: default, dragon
```

---

## Test Case 9: Inventář a kapacita

**ID:** TC009  
**Priorita:** Střední  
**Přiřazeno:** L  
**Komponenta:** InventoryCommand, TakeCommand  

**Popis:** Ověření limitu kapacity inventáře.

**Pre-kondice:**
- Hráč má prázdný inventář
- Max kapacita je 10 jednotek váhy
- V místnosti jsou těžké předměty

**Testovací kroky:**
1. Poslat příkaz: `take egg` (Weight: 8)
2. Ověřit úspěšné vzetí
3. Poslat příkaz: `take sword` (Weight: 2)
4. Ověřit úspěšné vzetí (celkem 10)
5. Zkusit vzít další předmět (např. scale Weight: 3)
6. Ověřit odpověď že inventář je plný

**Očekávaný výsledek:**
- Inventář sleduje celkovou váhu
- Nelze překročit maximální kapacitu
- Příkaz inventory ukazuje aktuální stav

---

## Test Case 10: Odemčení cesty klíčem

**ID:** TC010  
**Priorita:** Vysoká  
**Přiřazeno:** L  
**Komponenta:** GoCommand, WorldService  

**Popis:** Ověření mechaniky odemykání cest pomocí předmětů.

**Pre-kondice:**
- Hráč je v místnosti "hallway"
- Cesta na sever (do vault) je zamčená potřebou "rusty_key"
- Hráč nemá klíč

**Testovací kroky:**
1. Poslat příkaz: `go north`
2. Ověřit odpověď že cesta je zamčená
3. Jít do armory a vzít rusty_key
4. Vrátit se do hallway
5. Poslat příkaz: `go north`
6. Ověřit průchod a automatické spotřebování klíče
7. Zkusit projít znovu
8. Ověřit že cesta zůstala otevřená

**Očekávaný výsledek:**
- Zamčená cesta blokuje pohyb
- Klíč je automaticky použit při průchodu
- Klíč je odebrán z inventáře
- Cesta zůstane otevřená pro všechny hráče

---

## Test Case 11: Persistencia dat hráče

**ID:** TC011  
**Priorita:** Vysoká  
**Přiřazeno:** S  
**Komponenta:** PlayerService, WorldService  

**Popis:** Ověření že stav hráče je správně uložen a obnoven.

**Pre-kondice:**
- Hráč má nějaké předměty v inventáři
- Hráč je v určité místnosti

**Testovací kroky:**
1. Hráč vezme předměty a přesune se do jiné místnosti
2. Hráč se odpojí (quit)
3. Restartovat server (volitelné)
4. Hráč se znovu přihlásí
5. Poslat příkaz: `inventory`
6. Ověřit že předměty zůstaly zachovány
7. Poslat příkaz: `explore`
8. Ověřit že hráč je ve stejné místnosti

**Očekávaný výsledek:**
- Inventář je persistován mezi relacemi
- Pozice hráče je uložena
- Data jsou správně načtena z players.json

---

## Test Case 12: Vícenásobní hráči ve stejné místnosti

**ID:** TC012  
**Priorita:** Střední  
**Přiřazeno:** L  
**Komponenta:** Server, Player  

**Popis:** Ověření že více hráčů může být současně v jedné místnosti.

**Pre-kondice:**
- Server běží
- Tři hráči jsou připojeni

**Testovací kroky:**
1. PlayerA jde do místnosti "hallway"
2. PlayerB jde do místnosti "hallway"
3. PlayerC jde do místnosti "hallway"
4. Každý hráč pošle příkaz: `explore`
5. Ověřit že každý vidí ostatní hráče v seznamu "Players here"
6. PlayerA odejde (go south)
7. Ověřit že PlayerB a PlayerC dostanou notifikaci

**Očekávaný výsledek:**
- Více hráčů může být ve stejné místnosti
- Všichni vidí seznam ostatních hráčů
- Notifikace o příchodu/odchodu fungují

---

## Test Case 13: Help příkaz

**ID:** TC013  
**Priorita:** Nízká  
**Přiřazeno:** L  
**Komponenta:** HelpCommand  

**Popis:** Ověření nápovědy pro všechny dostupné příkazy.

**Pre-kondice:**
- Hráč je připojen k serveru

**Testovací kroky:**
1. Poslat příkaz: `help`
2. Ověřit že odpověď obsahuje seznam všech příkazů
3. Ověřit přítomnost: help, explore, go, inventory, take, drop, talk, say, whisper, broadcast, attack, trade, give
4. Ověřit čitelnost formátování

**Očekávaný výsledek:**
- Help zobrazí všechny registrované příkazy
- Formátování je přehledné
- Příkazy jsou popsány

---

## Test Case 14: Explore příkaz

**ID:** TC014  
**Priorita:** Vysoká  
**Přiřazeno:** L  
**Komponenta:** ExploreCommand  

**Popis:** Ověření detailního popisu místnosti.

**Pre-kondice:**
- Hráč je v libovolné místnosti

**Testovací kroky:**
1. Poslat příkaz: `explore`
2. Ověřit že odpověď obsahuje:
   - Název místnosti
   - Popis místnosti
   - Seznam východů (Exits)
   - Seznam předmětů (Items here)
   - Seznam NPC (Characters here)
   - Seznam ostatních hráčů (Players here)

**Očekávaný výsledek:**
- Všechny informace o místnosti jsou zobrazeny
- Formátování je konzistentní
- Prázdné sekce se nezobrazují nebo ukazují "none"

---

## Test Case 15: Give příkaz mezi hráči

**ID:** TC015  
**Priorita:** Střední  
**Přiřazeno:** S  
**Komponenta:** GiveCommand  

**Popis:** Ověření předávání předmětů mezi hráči.

**Pre-kondice:**
- PlayerA má předmět "rusty_key"
- PlayerB je ve stejné místnosti
- PlayerB nemá tento předmět

**Testovací kroky:**
1. PlayerA pošle: `give key to PlayerB`
2. Ověřit že PlayerA dostane potvrzení
3. Ověřit že PlayerB dostane notifikaci o přijetí
4. PlayerB pošle: `inventory`
5. Ověřit že PlayerB nyní má klíč
6. PlayerA pošle: `inventory`
7. Ověřit že PlayerA již klíč nemá

**Očekávaný výsledek:**
- Předmět je transferován mezi inventáři
- Oba hráči dostanou zpětnou vazbu
- Stav je persistován

---

## Test Case 16: Neplatné příkazy

**ID:** TC016  
**Priorita:** Nízká  
**Přiřazeno:** L  
**Komponenta:** Server (Command Handler)  

**Popis:** Ověření zpracování neznámých a špatně formátovaných příkazů.

**Pre-kondice:**
- Hráč je připojen

**Testovací kroky:**
1. Poslat příkaz: `invalidcommand`
2. Ověřit odpověď "Unknown command"
3. Poslat příkaz: `go` (bez argumentu)
4. Ověřit odpověď s usage informací
5. Poslat příkaz: `take` (bez argumentu)
6. Ověřit chybovou hlášku
7. Poslat prázdný řádek
8. Ověřit že server ignoruje nebo zobrazí prompt

**Očekávaný výsledek:**
- Neznámé příkazy jsou odmítnuty s helpful message
- Chybějící argumenty jsou detekovány
- Server nespadne při špatném vstupu

---

## Test Case 17: Kompletní herní cyklus (End-to-End)

**ID:** TC017  
**Priorita:** Vysoká  
**Přiřazeno:** L (koordinace), S (podpora)  
**Komponenta:** Celý systém  

**Popis:** Ověření kompletního průchodu hrou od začátku do vítězství.

**Pre-kondice:**
- Server je restartován (čistý stav)
- Dva hráči jsou připojeni

**Testovací kroky:**
1. Oba hráči se zaregistrují/přihlásí
2. PlayerA: Jít do armory, vzít sharp_sword a rusty_key
3. PlayerA: Jít přes hallway do vault, vzít fake_egg
4. PlayerA: Předat fake_egg PlayerB (nebo jít společně)
5. PlayerB: Jít do lair s fake_egg
6. PlayerB: Obchodovat s drakem (trade fake_egg → golden_egg)
7. PlayerA: Jít do tower, zabít boss_guard (attack knight)
8. PlayerA nebo B: Vzít dragon_scale
9. Jeden hráč donese golden_egg a dragon_scale do temple
10. Použít příkaz submit (nebo ekvivalent)
11. Ověřit vítěznou obrazovku pro všechny hráče

**Očekávaný výsledek:**
- Všechny mechaniky fungují společně
- Kooperace mezi hráči je možná
- Hra končí vítězstvím při splnění podmínek
- Možnost restartu hry

---

## Test Case 18: Odpojení a opětovné připojení hráče

**ID:** TC018  
**Priorita:** Střední  
**Přiřazeno:** L  
**Komponenta:** Networking, Player  

**Popis:** Ověření graceful disconnect a cleanup.

**Pre-kondice:**
- Hráč je připojen a má nějaké předměty
- Další hráč je ve stejné místnosti

**Testovací kroky:**
1. PlayerA odejde pomocí `quit`
2. Ověřit že PlayerB dostane notifikaci o odchodu
3. Server zavře spojení
4. PlayerA se znovu připojí
5. Ověřit že stav byl zachován (inventář, pozice)
6. Simulovat náhlé odpojení (kill procesu klienta)
7. Ověřit že server detekuje disconnect do 10 sekund

**Očekávaný výsledek:**
- Graceful quit funguje správně
- Stav hráče je zachován
- Náhlé odpojení je detekováno
- Resource cleanup proběhne

---

## Test Case 19: Load test - více simultánních klientů

**ID:** TC019  
**Priorita:** Nízká  
**Přiřazeno:** L  
**Komponenta:** Server, Networking  

**Popis:** Ověření stability serveru při větším počtu klientů.

**Pre-kondice:**
- Server běží

**Testovací kroky:**
1. Spustit 10 simultánních klientů (skriptem)
2. Všichni se přihlásí jako různí uživatelé
3. Všichni provedou příkaz `explore`
4. Všichni se přesunou do různé místnosti
5. Někteří si vymění předměty
6. Monitorovat paměť a CPU serveru
7. Postupně všechny odpojit

**Očekávaný výsledek:**
- Server zvládne 10+ klientů bez pádu
- Žádné race conditions
- Všechny příkazy jsou zpracovány
- Paměť je správně uvolňována

---

## Test Case 20: JSON data validita

**ID:** TC020  
**Priorita:** Vysoká  
**Přiřazeno:** S  
**Komponenta:** WorldService, PlayerService  

**Popis:** Ověření správného načítání a validace JSON souborů.

**Pre-kondice:**
- Existují rooms.json, items.json, npcs.json

**Testovací kroky:**
1. Startovat server
2. Ověřit že všechny místnosti jsou načteny (7 rooms)
3. Ověřit že všechny předměty jsou načteny (5 items)
4. Ověřit že všechny NPC jsou načteny (4 npcs)
5. Poškodit rooms.json (nevalidní JSON)
6. Restartovat server
7. Ověřit že server handle error gracefully
8. Opravit rooms.json
9. Restartovat server
10. Ověřit že vše funguje

**Očekávaný výsledek:**
- Validní JSON jsou správně načteny
- Nevalidní JSON nezpůsobí crash serveru
- Error messages jsou informativní
- Data jsou obnovitelná

---

## Shrnutí pokrytí testů

| Kategorie | Počet testů | Přiřazeno S | Přiřazeno L |
|-----------|-------------|-------------|-------------|
| Modely & Služby | 3 | 3 | 0 |
| Příkazy (S) | 5 | 5 | 0 |
| Příkazy (L) | 6 | 0 | 6 |
| Komunikace | 2 | 0 | 2 |
| Síť & Klient | 3 | 0 | 3 |
| Integrace & E2E | 1 | 0.5 | 0.5 |
| **Celkem** | **20** | **8.5** | **11.5** |

*Poznámka: Některé testy vyžadují spolupráci obou členů týmu.*

---

## Pokyny pro spouštění testů

### Manuální testování
1. Spustit server: `dotnet run --project onlineHra`
2. Spustit jednoho nebo více klientů: `dotnet run --project onlineHraClient`
3. Postupně provádět kroky z test case
4. Dokumentovat výsledky

### Automatizované testy (budoucí)
```bash
# Spustit unit testy
dotnet test onlineHra.Tests

# Spustit integrace testy
dotnet test onlineHra.Tests --filter "Category=Integration"

# Spustit všechny testy s coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## Kritéria acceptance

- [ ] Všechny testy s prioritou "Vysoká" musí projít
- [ ] Minimálně 80% testů celkově musí projít před odevzdáním
- [ ] Žádný critical bug nalezen během testování
- [ ] Performance testy ukazují stabilní server pro 10+ klientů
- [ ] Všechny edge cases jsou zdokumentovány
