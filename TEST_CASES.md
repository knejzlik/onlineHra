# Test Cases - Online MUD Game

Tento dokument obsahuje testovací případy pro online MUD hru. Testy jsou určeny pro manuální testování spolužáky a obsahují přesné návody, konkrétní testovací data a jednoznačné očekávané výsledky.

---

## Testovací účty

Před zahájením testování si připravte následující testovací účty. Účty vytvoříte během testování registrací.

| Typ hráče | Uživatelské jméno | Heslo | Stav před testem |
|-----------|-------------------|-------|------------------|
| Existující hráč | test_player | Test123 | Účet existuje, prázdný inventář, pozice "start" |
| Hráč s uloženým stavem | saved_player | Save123 | Má uložený předmět "sharp_sword", pozice "hallway" |
| Neexistující hráč | ghost_player_999 | — | Účet nesmí existovat |
| Hráč pro registraci | new_player_001 | New123 | Účet nesmí existovat |
| Druhý testovací hráč | test_player2 | Test456 | Účet existuje, prázdný inventář |

---

## Nastavení prostředí pro testování

- **Server IP:** 127.0.0.1 (localhost)
- **Server Port:** 65525
- **Klient:** onlineHraClient (spustit příkazem `dotnet run --project onlineHraClient`)
- **Server:** onlineHra (spustit příkazem `dotnet run --project onlineHra`)
- **Log file:** Data/server.log

---

## Rozdělení testů podle kategorií

| Kategorie | Počet testů | ID testů |
|-----------|-------------|----------|
| MVP funkce hry | 12 | TC001-TC012 |
| Povinné požadavky I1-I4, P1 | 5 | TC013-TC017 |
| Mechanika M1 - Kooperativní sbírání | 2 | TC018-TC019 |
| Mechanika M2 - Obchod s NPC | 2 | TC020-TC021 |
| Mechanika M3 - Boj s bossem | 2 | TC022-TC023 |
| Mechanika M4 - Společné vítězství | 2 | TC024-TC025 |
| Mechanika M5 - Persistentní svět | 2 | TC026-TC027 |
| **Celkem** | **27** | |

---

## MVP FUNKCE HRY (TC001-TC012)

### TC001: Připojení klienta k serveru

**ID:** TC001  
**Priorita:** Vysoká  
**Komponenta:** Networking/Server.cs  

**Popis:** Ověření že klient se dokáže úspěšně připojit k serveru.

**Pre-kondice:**
- Server je spuštěn příkazem `dotnet run --project onlineHra`
- Server zobrazuje zprávu o spuštění na portu 65525

**Testovací kroky:**
1. Spustit klienta příkazem `dotnet run --project onlineHraClient`
2. Na výzvu "Server address (default: localhost):" stisknout Enter
3. Na výzvu "Server port (default: 65525):" stisknout Enter
4. Čekat na připojení

**Očekávaný výsledek:**
- Klient zobrazí zprávu "Connecting to localhost:65525..."
- Následně se zobrazí zelená zpráva "Connected!"
- Server přijme připojení a zobrazí uvítací zprávu s možností registrace/přihlášení

**Testovací data:**
```
IP: 127.0.0.1
Port: 65525
```

---

### TC002: Připojení více klientů současně

**ID:** TC002  
**Priorita:** Vysoká  
**Komponenta:** Networking/Server.cs  

**Popis:** Ověření že server zvládne připojení více klientů najednou.

**Pre-kondice:**
- Server běží na portu 65525
- První klient je již připojen a přihlášen jako "test_player"

**Testovací kroky:**
1. Spustit druhého klienta příkazem `dotnet run --project onlineHraClient`
2. Připojit se na localhost:65525
3. Registrovat nového hráče: `register test_player2 Test456`
4. Přihlásit se: `login test_player2 Test456`
5. Spustit třetího klienta v dalším terminálu
6. Připojit se na localhost:65525
7. Registrovat: `register new_player_001 New123`

**Očekávaný výsledek:**
- Všichni tři klienti jsou úspěšně připojeni
- Každý klient vidí vlastní prompt ">>> "
- Server nezamrzl ani nespadl
- Každý klient může nezávisle posílat příkazy

**Testovací data:**
```
Klient 1: test_player / Test123
Klient 2: test_player2 / Test456
Klient 3: new_player_001 / New123
```

---

### TC003: Registrace nového hráče

**ID:** TC003  
**Priorita:** Vysoká  
**Komponenta:** Commands/RegisterOrLogin.cs, PlayerService  

**Popis:** Ověření registrace nového uživatelského účtu.

**Pre-kondice:**
- Server běží
- Klient je připojen
- Účet "new_player_001" neexistuje

**Testovací kroky:**
1. Poslat příkaz: `register new_player_001 New123`
2. Ověřit odpověď
3. Zkusit znovu registrovat stejného hráče: `register new_player_001 New123`

**Očekávaný výsledek:**
- První registrace vrátí: "Registration successful. You can now log in."
- Druhá registrace stejného hráče vrátí: "Username already exists."
- Hráč je uložen v souboru Data/players.json

**Testovací data:**
```
Username: new_player_001
Password: New123
```

---

### TC004: Přihlášení existujícího hráče

**ID:** TC004  
**Priorita:** Vysoká  
**Komponenta:** Commands/RegisterOrLogin.cs, PlayerService  

**Popis:** Ověření přihlášení s různými scénáři (správné/špatné heslo).

**Pre-kondice:**
- Server běží
- Účet "test_player" s heslem "Test123" existuje
- Klient je připojen

**Testovací kroky:**
1. Poslat příkaz: `login ghost_player_999 anypassword`
2. Ověřit odpověď
3. Poslat příkaz: `login test_player WrongPassword`
4. Ověřit odpověď
5. Poslat příkaz: `login test_player Test123`
6. Ověřit odpověď

**Očekávaný výsledek:**
- Krok 1: "User does not exist. Please register first."
- Krok 2: "Incorrect password."
- Krok 3: "Login successful. Welcome back, test_player!" a zobrazení místnosti

**Testovací data:**
```
Neexistující uživatel: ghost_player_999
Existující uživatel: test_player
Správné heslo: Test123
Špatné heslo: WrongPassword
```

---

### TC005: Pohyb mezi místnostmi

**ID:** TC005  
**Priorita:** Vysoká  
**Komponenta:** Commands/Go.cs  

**Popis:** Ověření pohybu hráče mezi propojenými místnostmi.

**Pre-kondice:**
- Hráč je přihlášen a nachází se v místnosti "start" (Cave Entrance)

**Testovací kroky:**
1. Poslat příkaz: `go north`
2. Ověřit odpověď a popis nové místnosti
3. Poslat příkaz: `go west`
4. Ověřit odpověď
5. Poslat příkaz: `go east`
6. Ověřit návrat do Main Hallway
7. Poslat příkaz: `go south`
8. Ověřit návrat do Cave Entrance
9. Poslat příkaz: `go up`

**Očekávaný výsledek:**
- Krok 1: "You go north..." + popis "Main Hallway"
- Krok 2: "You go west..." + popis "Armory"
- Krok 3: "You go east..." + popis "Main Hallway"
- Krok 4: "You go south..." + popis "Cave Entrance"
- Krok 5: "Cannot go up from here."

**Testovací data:**
```
Start: start (Cave Entrance)
Cesta: start → hallway → armory → hallway → start
```

---

### TC006: Zobrazení místnosti (Explore)

**ID:** TC006  
**Priorita:** Vysoká  
**Komponenta:** Commands/Explore.cs  

**Popis:** Ověření detailního popisu místnosti příkazem explore.

**Pre-kondice:**
- Hráč je v místnosti "armory"

**Testovací kroky:**
1. Poslat příkaz: `explore`
2. Zkontrolovat všechny sekce výstupu

**Očekávaný výsledek:**
Výstup obsahuje:
- Název: "=== Armory ==="
- Popis: "Old armory holding forgotten equipment."
- Exits: "east: Main Hallway"
- Items here: "Sharp Sword, Rusty Key"
- Characters here: "none" nebo seznam NPC
- Players here: seznam hráčů v místnosti

**Testovací data:**
```
Místnost: armory
Očekávané předměty: sharp_sword, rusty_key
```

---

### TC007: Sebrání předmětu (Take)

**ID:** TC007  
**Priorita:** Vysoká  
**Komponenta:** Commands/Take.cs  

**Popis:** Ověření příkazu take pro sběr předmětů z místnosti.

**Pre-kondice:**
- Hráč je v místnosti "armory"
- V místnosti jsou předměty: sharp_sword, rusty_key
- Hráč má prázdný inventář

**Testovací kroky:**
1. Poslat příkaz: `take sword`
2. Ověřit odpověď
3. Poslat příkaz: `inventory`
4. Ověřit obsah inventáře
5. Poslat příkaz: `take key`
6. Ověřit odpověď
7. Zkusit vzít neexistující předmět: `take hammer`

**Očekávaný výsledek:**
- Krok 1: "You picked up the Sharp Sword."
- Krok 2: Inventář obsahuje "Sharp Sword (Weight: 2)"
- Krok 3: "You picked up the Rusty Key."
- Krok 4: "There is no 'hammer' here."

**Testovací data:**
```
Místnost: armory
Předměty ke sběru: sharp_sword, rusty_key
Neexistující předmět: hammer
```

---

### TC008: Odložení předmětu (Drop)

**ID:** TC008  
**Priorita:** Střední  
**Komponenta:** Commands/Drop.cs  

**Popis:** Ověření příkazu drop pro odložení předmětů do místnosti.

**Pre-kondice:**
- Hráč má v inventáři "rusty_key"
- Hráč je v místnosti "hallway"

**Testovací kroky:**
1. Poslat příkaz: `drop key`
2. Ověřit odpověď
3. Poslat příkaz: `inventory`
4. Ověřit že klíč není v inventáři
5. Poslat příkaz: `explore`
6. Ověřit že klíč je v místnosti
7. Zkusit odhodit předmět který hráč nemá: `drop sword`

**Očekávaný výsledek:**
- Krok 1: "You dropped the Rusty Key."
- Krok 2: Inventář neobsahuje Rusty Key
- Krok 3: "Items here:" zahrnuje "Rusty Key"
- Krok 4: "You don't have 'sword' in your inventory."

**Testovací data:**
```
Předmět k odhození: rusty_key
Místnost: hallway
```

---

### TC009: Inventář a kontrola kapacity

**ID:** TC009  
**Priorita:** Střední  
**Komponenta:** Commands/Inventory.cs, Commands/Take.cs  

**Popis:** Ověření limitu kapacity inventáře (max 10 jednotek váhy).

**Pre-kondice:**
- Hráč má prázdný inventář
- Hráč je v místnosti "armory"
- K dispozici: sharp_sword (Weight: 2), rusty_key (Weight: 1)
- Hráč již získal fake_egg (Weight: 8) z vaultu

**Testovací kroky:**
1. Hráč vezme fake_egg (pokud ho ještě nemá, jít do vault a vzít)
2. Poslat příkaz: `inventory`
3. Ověřit váhu: 8/10
4. Jít do armory a vzít sword: `take sword`
5. Ověřit že byl přijat (celkem 10)
6. Zkusit vzít další předmět: `take key`

**Očekávaný výsledek:**
- Krok 2: Inventář ukazuje "Large Egg (Weight: 8)", celkem 8/10
- Krok 4: "You picked up the Sharp Sword."
- Krok 5: Inventář ukazuje celkovou váhu 10/10
- Krok 6: "Your inventory is full. Maximum capacity is 10."

**Testovací data:**
```
Max kapacita: 10
fake_egg Weight: 8
sharp_sword Weight: 2
rusty_key Weight: 1
```

---

### TC010: Rozhovor s NPC (Talk)

**ID:** TC010  
**Priorita:** Střední  
**Komponenta:** Commands/Talk.cs  

**Popis:** Ověření dialogového systému s NPC postavami.

**Pre-kondice:**
- Hráč je v místnosti "start"
- V místnosti je NPC "guide" (Wounded Soldier)

**Testovací kroky:**
1. Poslat příkaz: `talk soldier`
2. Ověřit odpověď
3. Poslat příkaz: `talk soldier about dragon`
4. Ověřit odpověď
5. Poslat příkaz: `talk nonexistent_npc`

**Očekávaný výsledek:**
- Krok 1: "Wounded Soldier says: 'Looking for the High Priest? The vault key and a weapon are in the armory to the west.'"
- Krok 2: "Wounded Soldier says: 'The dragon to the east won't give up her egg easily. Find the heavy fake one and try to trade.'"
- Krok 3: "There is no 'nonexistent_npc' here."

**Testovací data:**
```
NPC: guide (Wounded Soldier)
Triggery: default, dragon
```

---

### TC011: Zobrazení ostatních hráčů v místnosti

**ID:** TC011  
**Priorita:** Střední  
**Komponenta:** Networking/Server.cs, Commands/Explore.cs  

**Popis:** Ověření že hráči vidí ostatní hráče ve stejné místnosti.

**Pre-kondice:**
- PlayerA (test_player) je v místnosti "hallway"
- PlayerB (test_player2) se připojuje

**Testovací kroky:**
1. PlayerA stojí v "hallway"
2. PlayerB se přihlásí a jde do "hallway": `go north` (ze startu)
3. PlayerA pošle: `explore`
4. PlayerB pošle: `explore`
5. PlayerA odejde: `go west`
6. PlayerB pošle: `explore`

**Očekávaný výsledek:**
- Krok 2: PlayerA vidí: "test_player2 arrives."
- Krok 3: PlayerA vidí v "Players here:" jméno "test_player2"
- Krok 4: PlayerB vidí v "Players here:" jméno "test_player"
- Krok 5: PlayerB vidí: "test_player leaves west."
- Krok 6: PlayerB vidí "Players here: none"

**Testovací data:**
```
PlayerA: test_player
PlayerB: test_player2
Místnost: hallway
```

---

### TC012: Příkaz pomoc (Help)

**ID:** TC012  
**Priorita:** Nízká  
**Komponenta:** Commands/Help.cs  

**Popis:** Ověření nápovědy pro všechny dostupné příkazy.

**Pre-kondice:**
- Hráč je připojen a přihlášen

**Testovací kroky:**
1. Poslat příkaz: `help`

**Očekávaný výsledek:**
Výstup obsahuje seznam všech příkazů:
- help, explore, go (n/s/e/w/u/d), inventory, take, drop
- talk, say, whisper, broadcast
- attack, trade, give
- quit

Formátování je přehledné s krátkým popisem každého příkazu.

**Testovací data:**
```
Příkaz: help
```

---

## POVINNÉ POŽADAVKY (TC013-TC017)

### TC013: I1 - Načítání herního světa z externích souborů

**ID:** TC013  
**Priorita:** Vysoká  
**Požadavek:** I1  
**Komponenta:** WorldService  

**Popis:** Ověření že herní data jsou načítána z JSON souborů a nejsou natvrdo v kódu.

**Pre-kondice:**
- Server je vypnutý
- Existují soubory: Data/rooms.json, Data/items.json, Data/npcs.json

**Testovací kroky:**
1. Otevřít Data/rooms.json a ověřit že obsahuje 7 místností
2. Spustit server: `dotnet run --project onlineHra`
3. Přihlásit se a jít do různých místností
4. Ověřit že všechny místnosti odpovídají JSON datům
5. Vypnout server
6. Změnit v rooms.json popis místnosti "start" na "TESTOVACÍ POPIS"
7. Restartovat server
8. Přihlásit se a jít do "start"

**Očekávaný výsledek:**
- Krok 2: Server se spustí bez chyby, načte všechny místnosti
- Krok 3: Všechny místnosti mají správné názvy a popisy z JSON
- Krok 8: Místnost "start" zobrazuje "TESTOVACÍ POPIS" - změna bez rekompilace

**Testovací data:**
```
Soubory: Data/rooms.json, Data/items.json, Data/npcs.json
Počet místností: 7
Počet předmětů: 5
Počet NPC: 4
```

---

### TC014: I2 - Logování událostí

**ID:** TC014  
**Priorita:** Vysoká  
**Požadavek:** I2  
**Komponenta:** LoggingService  

**Popis:** Ověření že server ukládá logy důležitých událostí do souboru.

**Pre-kondice:**
- Server běží
- Soubor Data/server.log existuje nebo bude vytvořen

**Testovací kroky:**
1. Smazat obsah Data/server.log (pokud existuje)
2. Přihlásit se jako test_player
3. Provést několik příkazů: `go north`, `take sword`, `explore`
4. Odpojit se: `quit`
5. Otevřít Data/server.log a zkontrolovat obsah

**Očekávaný výsledek:**
Log obsahuje záznamy s časovými značkami:
- "[timestamp] [Info] Player 'test_player' connected"
- "[timestamp] [Command] [test_player] executed: go north"
- "[timestamp] [Command] [test_player] executed: take sword"
- "[timestamp] [Command] [test_player] executed: explore"
- "[timestamp] [Info] Player 'test_player' disconnected"

Formát: `[YYYY-MM-DD HH:mm:ss] [Level] Message`

**Testovací data:**
```
Log file: Data/server.log
Uživatel: test_player
Příkazy: go north, take sword, explore, quit
```

---

### TC015: I3 - Přihlášení a persistence hráče

**ID:** TC015  
**Priorita:** Vysoká  
**Požadavek:** I3  
**Komponenta:** PlayerService  

**Popis:** Ověření že stav hráče je uložen při odpojení a obnoven při přihlášení.

**Pre-kondice:**
- Účet "saved_player" s heslem "Save123" existuje
- Hráč nemá žádné předměty

**Testovací kroky:**
1. Přihlásit se: `login saved_player Save123`
2. Jít do armory: `go north`, `go west`
3. Vzít předmět: `take sword`
4. Odpojit se: `quit`
5. Restartovat server (volitelné pro test persistence)
6. Znovu se přihlásit: `login saved_player Save123`
7. Poslat příkaz: `inventory`
8. Poslat příkaz: `explore`

**Očekávaný výsledek:**
- Krok 7: Inventář obsahuje "Sharp Sword"
- Krok 8: Hráč je v místnosti "armory" (poslední pozice)
- Data jsou uložena v Data/players.json

**Testovací data:**
```
Username: saved_player
Password: Save123
Uložený předmět: sharp_sword
Uložená pozice: armory
```

---

### TC016: I4 - Vlastní klientský program

**ID:** TC016  
**Priorita:** Vysoká  
**Požadavek:** I4  
**Komponenta:** onlineHraClient/Program.cs  

**Popis:** Ověření funkčnosti vlastního klientského programu.

**Pre-kondice:**
- Server běží na portu 65525

**Testovací kroky:**
1. Spustit klienta: `dotnet run --project onlineHraClient`
2. Zadat server adresu (Enter pro localhost)
3. Zadat port (Enter pro 65525)
4. Ověřit že se klient připojí
5. Registrovat se nebo přihlásit
6. Poslat několik příkazů: `help`, `explore`, `go north`
7. Ověřit barevné zvýraznění výstupu
8. Odpojit se příkazem `quit`

**Očekávaný výsledek:**
- Klient se úspěšně připojí
- Zobrazuje vstupní prompt ">>> "
- Příkazy jsou odesílány serveru
- Výstup ze serveru je barevně zvýrazněn:
  - Zelená: úspěšné akce, vítězství
  - Červená: chyby, boj
  - Žlutá: informace o místnosti
  - Tyrkysová: chat zprávy
- Po zadání `quit` se klient korektně odpojí

**Testovací data:**
```
Klient: onlineHraClient
Server: localhost:65525
```

---

### TC017: P1 - Dokončení hry

**ID:** TC017  
**Priorita:** Vysoká  
**Požadavek:** P1  
**Komponenta:** Commands/Give.cs  

**Popis:** Ověření že hru lze dokončit splněním cíle (odevzdání artefaktů).

**Pre-kondice:**
- Server je restartován (čistý stav, žádné odevzdané předměty)
- Dva hráči jsou připojeni: PlayerA (test_player), PlayerB (test_player2)
- PlayerA má sharp_sword
- PlayerB má fake_egg

**Testovací kroky:**
1. PlayerB jde do lair: `go north`, `go east`
2. PlayerB obchoduje s drakem: `trade egg`
3. PlayerB získá golden_egg
4. PlayerA jde do tower: `go north`, `go up`
5. PlayerA zabije bosse: `attack knight`
6. PlayerA vezme dragon_scale: `take scale`
7. PlayerA jde do temple: `go up`
8. PlayerA odevzdá dragon_scale: `give scale`
9. PlayerB jde do temple: `go north`, `go up`, `go up`
10. PlayerB odevzdá golden_egg: `give egg`

**Očekávaný výsledek:**
- Krok 2: "You successfully traded the Large Egg for the Golden Egg."
- Krok 5: "You strike down Dark Knight! They drop Dragon Scale."
- Krok 8: "You handed over the Dragon Scale. The High Priest still needs the other artifact."
- Krok 10: "You handed over the Golden Egg." + broadcast všem hráčům
- Všichni hráči vidí: "=== CONGRATULATIONS! ===" a zprávu o vítězství
- Hra je dokončena

**Testovací data:**
```
Cílové předměty: golden_egg, dragon_scale
Cílová místnost: temple
Finální příkaz: give <item>
```

---

## MECHANIKA M1 - KOOPERATIVNÍ SBÍRÁNÍ PŘEDMĚTŮ (TC018-TC019)

### TC018: M1 - Předávání předmětu mezi hráči (Give)

**ID:** TC018  
**Priorita:** Střední  
**Mechanika:** M1  
**Komponenta:** Commands/GiveCommand.cs  

**Popis:** Ověření že hráči mohou spolupracovat předáváním předmětů.

**Pre-kondice:**
- PlayerA (test_player) má rusty_key
- PlayerB (test_player2) je ve stejné místnosti "hallway"
- PlayerB nemá rusty_key

**Testovací kroky:**
1. PlayerA pošle: `give key to test_player2`
2. PlayerB ověří přijetí zprávy
3. PlayerB pošle: `inventory`
4. PlayerA pošle: `inventory`

**Očekávaný výsledek:**
- Krok 1: PlayerA vidí "You gave Rusty Key to test_player2."
- Krok 2: PlayerB vidí "test_player gave you Rusty Key."
- Krok 3: PlayerB má v inventáři "Rusty Key"
- Krok 4: PlayerA nemá v inventáři "Rusty Key"

**Testovací data:**
```
PlayerA: test_player
PlayerB: test_player2
Předmět: rusty_key
Místnost: hallway
```

---

### TC019: M1 - Kooperativní odemykání cesty

**ID:** TC019  
**Priorita:** Střední  
**Mechanika:** M1  
**Komponenta:** Commands/Go.cs  

**Popis:** Ověření že jeden hráč může odemknout cestu pro ostatní.

**Pre-kondice:**
- Cesta na sever z "hallway" do "vault" je zamčená (vyžaduje rusty_key)
- PlayerA má rusty_key
- PlayerB nemá klíč a čeká v "hallway"

**Testovací kroky:**
1. PlayerB zkusí jít na sever: `go north`
2. Ověřit zamčení
3. PlayerA jde na sever: `go north` (klíč se spotřebuje)
4. PlayerB zkusí znovu jít na sever: `go north`

**Očekávaný výsledek:**
- Krok 1: "The path to the north is locked. You need: Rusty Key."
- Krok 3: PlayerA projde, klíč je odstraněn z inventáře
- Krok 4: PlayerB nyní může projít bez klíče (cesta zůstala otevřená)

**Testovací data:**
```
Zamčená cesta: hallway → vault (north)
Potřebný předmět: rusty_key
```

---

## MECHANIKA M2 - OBCHOD S NPC (TC020-TC021)

### TC020: M2 - Úspěšný obchod s drakem

**ID:** TC020  
**Priorita:** Vysoká  
**Mechanika:** M2  
**Komponenta:** Commands/TradeCommand.cs  

**Popis:** Ověření mechaniky obchodu s NPC drakem.

**Pre-kondice:**
- Hráč má v inventáři fake_egg (Large Egg)
- Hráč je v místnosti "lair" (Dragon Lair)
- V místnosti je NPC "dragon"

**Testovací kroky:**
1. Poslat příkaz: `trade egg`
2. Ověřit odpověď
3. Poslat příkaz: `inventory`

**Očekávaný výsledek:**
- Krok 1: "You successfully traded the Large Egg for the Golden Egg."
- Krok 2: fake_egg je odstraněn z inventáře
- Krok 3: golden_egg je přidán do inventáře

**Testovací data:**
```
NPC: dragon (Dragon Mother)
Místnost: lair
Dát: fake_egg (Weight: 8)
Získat: golden_egg (Weight: 5)
```

---

### TC021: M2 - Neúspěšný obchod se špatným předmětem

**ID:** TC021  
**Priorita:** Střední  
**Mechanika:** M2  
**Komponenta:** Commands/TradeCommand.cs  

**Popis:** Ověření že drak nepřijme jiný předmět než fake_egg.

**Pre-kondice:**
- Hráč má v inventáři sharp_sword
- Hráč je v místnosti "lair"

**Testovací kroky:**
1. Poslat příkaz: `trade sword`
2. Ověřit odpověď

**Očekávaný výsledek:**
- Krok 1: "The dragon is not interested in this item." nebo podobná hláška
- sharp_sword zůstane v inventáři
- Žádná transakce neproběhne

**Testovací data:**
```
NPC: dragon
Nabízený předmět: sharp_sword
Očekávaný předmět: fake_egg
```

---

## MECHANIKA M3 - BOJ S BOSSEM (TC022-TC023)

### TC022: M3 - Útok na NPC bez zbraně

**ID:** TC022  
**Priorita:** Střední  
**Mechanika:** M3  
**Komponenta:** Commands/AttackCommand.cs  

**Popis:** Ověření že útok bez zbraně je neefektivní nebo zakázaný.

**Pre-kondice:**
- Hráč je v místnosti "tower"
- V místnosti je NPC "boss_guard" (Dark Knight)
- Hráč NEMÁ sharp_sword v inventáři

**Testovací kroky:**
1. Poslat příkaz: `attack knight`
2. Ověřit odpověď

**Očekávaný výsledek:**
- Krok 1: "You need a weapon to fight!" nebo "Your bare hands are no match for Dark Knight."
- NPC není poraženo
- Hráč nedostane žádnou odměnu

**Testovací data:**
```
NPC: boss_guard (Dark Knight)
HP: 10
Hráčova zbraň: žádná
```

---

### TC023: M3 - Útok na NPC se zbraní

**ID:** TC023  
**Priorita:** Vysoká  
**Mechanika:** M3  
**Komponenta:** Commands/AttackCommand.cs  

**Popis:** Ověření úspěšného boje s bossem pomocí zbraně.

**Pre-kondice:**
- Hráč má v inventáři sharp_sword
- Hráč je v místnosti "tower"
- NPC "boss_guard" je živé

**Testovací kroky:**
1. Poslat příkaz: `attack knight`
2. Ověřit odpověď
3. Poslat příkaz: `explore`
4. Ověřit že NPC je mrtvé a droplo předmět
5. Poslat příkaz: `take scale`
6. Zkusit znovu zaútočit: `attack knight`

**Očekávaný výsledek:**
- Krok 1: "You strike down Dark Knight with your Sharp Sword! They drop Dragon Scale."
- Krok 3: NPC je označeno jako mrtvé nebo zmizelo
- Krok 4: "Items here:" obsahuje "Dragon Scale"
- Krok 5: "You picked up the Dragon Scale."
- Krok 6: "Dark Knight is already dead."

**Testovací data:**
```
NPC: boss_guard
Zbraň: sharp_sword
Drop: dragon_scale
```

---

## MECHANIKA M4 - SPOLEČNÉ VÍTĚZSTVÍ (TC024-TC025)

### TC024: M4 - Částečné splnění podmínky vítězství

**ID:** TC024  
**Priorita:** Střední  
**Mechanika:** M4  
**Komponenta:** Commands/GiveCommand.cs  

**Popis:** Ověření že odevzdání pouze jednoho artefaktu nestačí k vítězství.

**Pre-kondice:**
- Server je restartován (žádné odevzdané předměty)
- Hráč má dragon_scale
- Hráč je v místnosti "temple"

**Testovací kroky:**
1. Poslat příkaz: `give scale`
2. Ověřit odpověď
3. Zkontrolovat zda hra skončila vítězstvím

**Očekávaný výsledek:**
- Krok 1: "You handed over the Dragon Scale. The High Priest still needs the other artifact."
- Hra NEKONČÍ vítězstvím
- Ostatní hráči vidí broadcast: "test_player has given the Dragon Scale!"

**Testovací data:**
```
Místnost: temple
Odevzdaný předmět: dragon_scale
Chybějící předmět: golden_egg
```

---

### TC025: M4 - Kompletní vítězství všech hráčů

**ID:** TC025  
**Priorita:** Vysoká  
**Mechanika:** M4  
**Komponenta:** Commands/GiveCommand.cs  

**Popis:** Ověření že všichni hráči vyhrají společně po odevzdání obou artefaktů.

**Pre-kondice:**
- Jeden artefakt (dragon_scale) již byl odevzdán jiným hráčem
- Hráč má golden_egg
- Hráč je v místnosti "temple"

**Testovací kroky:**
1. Poslat příkaz: `give egg`
2. Ověřit odpověď
3. Všichni připojení hráči ověří vítěznou zprávu

**Očekávaný výsledek:**
- Krok 1: "You handed over the Golden Egg."
- Všichni hráči vidí:
  ```
  === CONGRATULATIONS! ===
  The ritual is complete!
  test_player and test_player2 have saved the kingdom!
  ```
- Hra je dokončena, možnost restartu

**Testovací data:**
```
Artefakty: dragon_scale + golden_egg
Místnost: temple
Vítězná zpráva: "CONGRATULATIONS"
```

---

## MECHANIKA M5 - PERSISTENTNÍ SVĚT (TC026-TC027)

### TC026: M5 - Uložení stavu při odpojení

**ID:** TC026  
**Priorita:** Vysoká  
**Mechanika:** M5  
**Komponenta:** PlayerService  

**Popis:** Ověření že stav hráče je uložen do JSON při odpojení.

**Pre-kondice:**
- Hráč "saved_player" je přihlášen
- Hráč má nějaké předměty v inventáři
- Hráč je v jiné místnosti než "start"

**Testovací kroky:**
1. Hráč vezme předměty a přesune se do "armory"
2. Poslat příkaz: `inventory` a zaznamenat obsah
3. Odpojit se: `quit`
4. Otevřít Data/players.json a najít záznam pro "saved_player"
5. Zkontrolovat CurrentRoomId a Inventory

**Očekávaný výsledek:**
- Krok 4: players.json obsahuje:
  ```json
  {
    "Username": "saved_player",
    "CurrentRoomId": "armory",
    "Inventory": ["sharp_sword", ...],
    ...
  }
  ```
- Data jsou uložena okamžitě po odpojení

**Testovací data:**
```
Username: saved_player
Očekávaná pozice: armory
Soubor: Data/players.json
```

---

### TC027: M5 - Obnovení stavu při přihlášení

**ID:** TC027  
**Priorita:** Vysoká  
**Mechanika:** M5  
**Komponenta:** PlayerService  

**Popis:** Ověření že stav hráče je obnoven z JSON při přihlášení.

**Pre-kondice:**
- Hráč "saved_player" byl dříve odpojen s uloženým stavem
- Hráč měl předměty a byl v místnosti "armory"

**Testovací kroky:**
1. Restartovat server (pro jistotu čistého stavu v paměti)
2. Přihlásit se: `login saved_player Save123`
3. Poslat příkaz: `inventory`
4. Poslat příkaz: `explore`

**Očekávaný výsledek:**
- Krok 3: Inventář obsahuje všechny předměty které hráč měl před odpojením
- Krok 4: Hráč je v místnosti "armory" (nebo kde byl při odpojení)
- Pozice a inventář jsou identické s předchozí relací

**Testovací data:**
```
Username: saved_player
Password: Save123
Očekávaná pozice: armory
Očekávané předměty: dle předchozího uložení
```

---

## HRANICNÍ PŘÍPADY A CHYBOVÉ STAVY

### TC028: Neplatné směry pohybu

**ID:** TC028  
**Priorita:** Nízká  
**Komponenta:** Commands/Go.cs  

**Popis:** Ověření zpracování neplatných směrů.

**Pre-kondice:**
- Hráč je v libovolné místnosti

**Testovací kroky:**
1. Poslat příkaz: `go northeast`
2. Poslat příkaz: `go left`
3. Poslat příkaz: `go`
4. Poslat příkaz: `go xyz`

**Očekávaný výsledek:**
- Všechny příkazy vrátí: "Cannot go <direction> from here." nebo "Usage: go <direction>"
- Server nespadne
- Hráč zůstane ve stejné místnosti

**Testovací data:**
```
Neplatné směry: northeast, left, xyz, (prázdný)
```

---

### TC029: Take/Drop neexistujícího předmětu

**ID:** TC029  
**Priorita:** Nízká  
**Komponenta:** Commands/Take.cs, Commands/Drop.cs  

**Popis:** Ověření zpracování neexistujících předmětů.

**Pre-kondice:**
- Hráč je v libovolné místnosti
- Hráč nemá předmět "magic_wand"

**Testovací kroky:**
1. Poslat příkaz: `take magic_wand`
2. Poslat příkaz: `drop magic_wand`

**Očekávaný výsledek:**
- Krok 1: "There is no 'magic_wand' here."
- Krok 2: "You don't have 'magic_wand' in your inventory."
- Server nespadne

**Testovací data:**
```
Neexistující předmět: magic_wand
```

---

### TC030: Komunikace s neexistujícím NPC

**ID:** TC030  
**Priorita:** Nízká  
**Komponenta:** Commands/Talk.cs, Commands/Attack.cs  

**Popis:** Ověření zpracování neexistujících NPC.

**Pre-kondice:**
- Hráč je v místnosti bez NPC "wizard"

**Testovací kroky:**
1. Poslat příkaz: `talk wizard`
2. Poslat příkaz: `attack wizard`

**Očekávaný výsledek:**
- Krok 1: "There is no 'wizard' here."
- Krok 2: "There is no 'wizard' here." nebo "Invalid target."
- Server nespadne

**Testovací data:**
```
Neexistující NPC: wizard
```

---



## POKYNY PRO SPOUŠTĚNÍ TESTŮ

### Postup pro testera

1. **Příprava prostředí:**
   - Otevřít dva terminály
   - V prvním terminálu spustit server: `dotnet run --project onlineHra`
   - Ve druhém terminálu spustit klienta: `dotnet run --project onlineHraClient`

2. **Postup testování:**
   - Projít testy v pořadí podle ID
   - U každého testu vyplnit skutečný výsledek
   - Označit test jako PASS/FAIL

3. **Dokumentace výsledků:**
   - Pokud test selže, zaznamenat přesnou chybovou hlášku
   - Pokud server spadne, restartovat a pokračovat

### Šablona pro záznam výsledků

```markdown
## Výsledky testování

| Test ID | Datum | Tester | Výsledek | Poznámky |
|---------|-------|--------|----------|----------|
| TC001 | | | PASS/FAIL | |
| TC002 | | | PASS/FAIL | |
...
```

---

