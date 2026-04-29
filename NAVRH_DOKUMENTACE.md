# Návrhový dokument - MUD Online Hra

> Tento dokument není určen ke schválení předem. Má sloužit především vám — jako nástroj pro koordinaci práce, sdílenou představu o řešení a zaznamenání důležitých rozhodnutí během vývoje. Součástí odevzdání bude až v závěrečné fázi projektu.

---

## 1. Rozdělení práce ve dvojici

### Člověk S (Server & Backend)

| Oblast | Konkrétní úkoly | Soubory |
|--------|----------------|---------|
| **Serverová logika** | Implementace TCP serveru, přijímání klientů, správa připojení | `Networking/Server.cs` |
| **Správa hráčů** | Registrace, přihlašování, ukládání stavu hráčů | `Services/PlayerService.cs`, `Models/PlayerState.cs`, `Networking/Player.cs` |
| **Herní svět** | Načítání místností, předmětů, NPC z JSON souborů | `Services/WorldService.cs`, `Models/Room.cs`, `Models/Item.cs`, `Models/Npc.cs` |
| **Příkazy - boj a trade** | Implementace attack, trade, give mechanik | `Commands/AttackCommand.cs`, `Commands/TradeCommand.cs`, `Commands/GiveCommand.cs` |
| **Logging** | Systém logování událostí serveru | `Services/LoggingService.cs` |
| **Win podmínka** | Detekce vítězství, restart hry | `Server.cs` (TriggerWin, RestartGame) |

### Člověk L (Klient & Komunikace)

| Oblast | Konkrétní úkoly | Soubory |
|--------|----------------|---------|
| **Klientská aplikace** | TCP klient, čtení/zápis zpráv, UI konzole | `onlineHraClient/Program.cs` |
| **Komunikační protokol** | Formát zpráv, parsing příkazů, odpovědi serveru | `Commands/ICommand.cs`, `Server.cs` (ExecuteCommand) |
| **Příkazy - pohyb** | Implementace go, explore mechanik | `Commands/Go.cs`, `Commands/Explore.cs` |
| **Příkazy - interakce** | Implementace take, drop, inventory | `Commands/Take.cs`, `Commands/Drop.cs`, `Commands/Inventory.cs` |
| **Příkazy - komunikace** | Implementace say, whisper, broadcast, talk | `Commands/Say.cs`, `Commands/Whisper.cs`, `Commands/Broadcast.cs`, `Commands/Talk.cs` |
| **Příkazy - ostatní** | Help, register/login | `Commands/Help.cs`, `Commands/RegisterOrLogin.cs` |
| **Testování** | Testování klienta, manuální testy komunikace | - |

### Přehled rozdělení

```
┌─────────────────────────────────────────────────────────────┐
│                    ROZDĚLENÍ PRÁCE                          │
├──────────────────────────┬──────────────────────────────────┤
│      ČLOVĚK S            │         ČLOVĚK L                 │
│      (Server)            │         (Klient + Protokol)      │
├──────────────────────────┼──────────────────────────────────┤
│ • TCP Server             │ • TCP Klient                     │
│ • PlayerService          │ • Komunikační protokol           │
│ • WorldService           │ • Příkazy: go, explore           │
│ • Attack/Trade/Give      │ • Příkazy: take, drop, inventory │
│ • Logging                │ • Příkazy: say, whisper, talk    │
│ • Win podmínka           │ • Příkazy: help, register/login  │
│ • Správa připojení       │ • UI/UX klienta                  │
│ • Data modely            │ • Testování                      │
└──────────────────────────┴──────────────────────────────────┘
```

---

## 2. Návrh architektury

### Blokové schéma architektury

```
┌─────────────────────────────────────────────────────────────────────┐
│                           KLIENTI (TCP)                             │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐              │
│  │  Klient 1   │    │  Klient 2   │    │  Klient N   │              │
│  │  Program.cs │    │  Program.cs │    │  Program.cs │              │
│  └──────┬──────┘    └──────┬──────┘    └──────┬──────┘              │
│         │                  │                  │                      │
│         └──────────────────┼──────────────────┘                      │
│                            │ TCP/IP                                 │
└────────────────────────────┼─────────────────────────────────────────┘
                             │
                    ┌────────▼────────┐
                    │     SERVER      │
                    │   (TcpListener) │
                    └────────┬────────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
        ▼                    ▼                    ▼
┌───────────────┐   ┌───────────────┐   ┌───────────────┐
│   Networking  │   │   Services    │   │   Commands    │
│               │   │               │   │               │
│ • Server.cs   │   │ • WorldSvc    │   │ • Go          │
│ • Player.cs   │   │ • PlayerSvc   │   │ • Explore     │
└───────────────┘   │ • LoggingSvc  │   │ • Take/Drop   │
                    └───────────────┘   │ • Attack      │
                                        │ • Trade       │
        ┌───────────────┐               │ • Give        │
        │    Models     │               │ • Say/Talk    │
        │               │               └───────────────┘
        │ • PlayerState │
        │ • Room        │               ┌───────────────┐
        │ • Item        │               │     Data      │
        │ • Npc         │               │               │
        └───────────────┘               │ • rooms.json  │
                                        │ • items.json  │
                                        │ • npcs.json   │
                                        └───────────────┘
```

### Diagram závislostí komponent

```
Server.cs
    ├── Player.cs (správa připojených hráčů)
    ├── WorldService (herní svět)
    ├── PlayerService (uložení hráčů)
    ├── LoggingService (logování)
    └── ICommand[] (příkazy)

WorldService
    ├── Room[] (místnosti)
    ├── Item[] (předměty)
    └── Npc[] (postavy)

PlayerService
    └── PlayerState[] (stavy hráčů)

Klient (onlineHraClient)
    └── TcpClient → Server
```

---

## 3. Popis komunikace mezi klientem a serverem

### Protokol

**Typ komunikace:** Textový protokol přes TCP/IP  
**Port:** 65525  
**Formát zpráv:** Jednoduché textové řádky ukončené newline (`\n`)

### Formát zpráv

#### Klient → Server
```
<příkaz> [argumenty]
```

Příklady:
```
help
go north
take sharp_sword
attack dragon
trade fake_egg
give golden_egg
say Ahoj všichni!
```

#### Server → Klient
```
<textová odpověď>
```

Příklady:
```
Welcome to the MUD game!
You stand at the entrance. A large gate leads north.
Exits: north
Items here: sharp_sword, rusty_key
You attacked with your sword. Dragon Mother roared and fell dead!
```

### Základní typy příkazů a odpovědí

| Kategorie | Příkazy | Příklad odpovědi |
|-----------|---------|------------------|
| **Navigace** | `go <směr>`, `explore` | Popis místnosti, východy |
| **Interakce s předměty** | `take <item>`, `drop <item>`, `inventory` | Potvrzení převzetí/vložení, seznam inventáře |
| **Boj** | `attack <npc>` | Výsledek útoku, HP nepřátel |
| **Obchod** | `trade <item>` | Výsledek obchodu |
| **Quest** | `give <item>` | Potvrzení předání, progress questu |
| **Komunikace** | `say <text>`, `whisper <text>`, `broadcast <text>`, `talk <npc>` | Zpráva hráčům/NPC |
| **Systém** | `help`, `login`, `register` | Nápověda, potvrzení přihlášení |

### Ukázka konkrétní komunikace

```
=== PŘIHLÁŠENÍ ===
Server: Do you want to login or register?
Klient: login
Server: Enter username:
Klient: player1
Server: Enter password:
Klient: *****
Server: Login successful!
Server: Welcome to the MUD game!
Server: You stand at the entrance. A large gate leads north.
Server: Exits: north
Server: Characters here: Wounded Soldier

=== HRANÍ ===
Klient: go north
Server: Large crossroad. Paths lead in all directions.
Server: Exits: south, west, north, east, up

Klient: go west
Server: Old armory holding forgotten equipment.
Server: Items here: sharp_sword, rusty_key

Klient: take sharp_sword
Server: You picked up Sharp Sword.

Klient: go east
Klient: go east
Server: Dragon's nest. Extremely hot.
Server: Characters here: Dragon Mother

Klient: trade fake_egg
Server: You handed the Heavy Egg to the dragon. The dragon was overjoyed 
        and let you take the real Golden Egg in return!

Klient: go west
Klient: go up
Server: Temple antechamber. Stairs lead up, but a boss blocks the way.
Server: Characters here: Dark Knight

Klient: attack dark knight
Server: You attacked with your sword. Dark Knight roared and fell dead!
        A Dragon Scale dropped from the body.

Klient: go up
Server: Sacred destination and end of the journey.
Server: Characters here: High Priest

Klient: give golden_egg
Server: You handed over the Golden Egg. The High Priest still needs 
        the other artifact.

Klient: give dragon_scale
Server: =========================================================
Server:                    CONGRATULATIONS!                     
Server:       The ritual is complete! Everyone wins!            
Server: =========================================================
```

---

## 4. Náčrt herního světa

### Téma hry

**Fantasy MUD hra** - Hráči procházejí fantasy světem s cílem získat artefakty pro dokončení rituálu.

### Struktura světa

**Počet místností:** 7

```
                    ┌─────────────┐
                    │   TEMPLE    │ ← Cíl hry (High Priest)
                    │   (tower↑)  │
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐
                    │   TOWER     │ ← Boss (Dark Knight)
                    │ (hallway↓)  │
                    └──────┬──────┘
                           │
    ┌──────────┐    ┌──────▼──────┐    ┌──────────┐
    │  ARMORY  │ ←─ │  HALLWAY    │ ─→ │   LAIR   │
    │(hallway→)│    │(crossroads) │    │(hallway←)│
    └──────────┘    └──────┬──────┘    └────┬─────┘
                           │               │
                    ┌──────▼──────┐        │
                    │    VAULT    │        │
                    │(hallway↑)   │        │
                    └─────────────┘        │
                                           │
                           ┌───────────────┘
                           │
                    ┌──────▼──────┐
                    │    START    │ ← Začátek (Guide NPC)
                    │(hallway↓)   │
                    └─────────────┘
```

### Popis místností

| ID | Název | Popis | Východy | Předměty | NPC |
|----|-------|-------|---------|----------|-----|
| `start` | Cave Entrance | Vstup do jeskyně | north → hallway | - | guide (Wounded Soldier) |
| `hallway` | Main Hallway | Křižovatka chodeb | south, west, north, east, up | - | - |
| `armory` | Armory | Stará zbrojnice | east → hallway | sharp_sword, rusty_key | - |
| `vault` | Locked Vault | Temná komora s artefakty | south → hallway | fake_egg | - |
| `lair` | Dragon Lair | Dračí doupě | west → hallway | - | dragon (Dragon Mother) |
| `tower` | Watchtower | Strážní věž s bossem | down → hallway, up → temple | - | boss_guard (Dark Knight) |
| `temple` | Temple | Posvátný chrám | down → tower | - | high_priest (High Priest) |

### Typy prvků ve hře

#### Předměty (Items)
| ID | Název | Popis | Váha | Účel |
|----|-------|-------|------|------|
| `sharp_sword` | Sharp Sword | Ostrý meč pro boj | 2 | Nutný pro útok na NPC |
| `rusty_key` | Rusty Key | Starý rezavý klíč | 1 | Odemyká severní východ z hallway |
| `fake_egg` | Large Egg | Těžké vejce | 8 | Obchod s drakem |
| `golden_egg` | Golden Egg | Skutečné dračí vejce | 5 | Artefakt pro rituál |
| `dragon_scale` | Dragon Scale | Svítící dračí šupina | 3 | Artefakt pro rituál |

#### Postavy (NPCs)
| ID | Jméno | Popis | HP | Útok | Funkce |
|----|-------|-------|-----|------|--------|
| `guide` | Wounded Soldier | Zraněný voják u vstupu | 0 | 0 | Rady hráčům |
| `dragon` | Dragon Mother | Drak střežící vejce | 0 | 0 | Obchod (fake_egg → golden_egg) |
| `boss_guard` | Dark Knight | Temný rytíř | 10 | 5 | Blokuje cestu, nutno porazit |
| `high_priest` | High Priest | Velekněz v chrámu | 0 | 0 | Přijímá artefakty |

---

## 5. Volba herních mechanik

### Vybrané mechaniky pro další fázi

| Mechanika | Popis | Důvod výběru |
|-----------|-------|--------------|
| **1. Kooperativní sbírání předmětů** | Hráči musí spolupracovat na získání všech artefaktů | Podporuje multiplayer aspekt, každý může mít jinou roli |
| **2. Obchod s NPC** | Speciální item lze vyměnit s drakem za jiný | Přidává strategickou vrstvu, hráč musí najít správný předmět |
| **3. Boj s bossem** | Nutnost porazit strážce pomocí nalezené zbraně | Klasická RPG mechanika, odměna za průzkum světa |
| **4. Společné vítězství** | Všichni hráči vyhrají, když jsou dodány oba artefakty | Motivuje ke spolupráci místo kompetice |
| **5. Persistentní svět** | Stav hráče se ukládá mezi relacemi | Umožňuje pokračovat později, realističtější zážitek |

### Zdůvodnění výběru

1. **Kooperativní aspekt** - Hra je navržena pro více hráčů současně. Všichni hráči vidí akce ostatních (pomocí broadcast zpráv) a společně pracují na cíli.

2. **Progressivní obtížnost** - Hráč musí nejprve prozkoumat svět, najít klíč a zbraň, pak obchodovat s drakem, porazit bosse a nakonec doručit artefakty.

3. **Jednoduchost implementace** - Vybrané mechaniky jsou dostatečně jednoduché na textovou implementaci, ale poskytují zábavný herní zážitek.

4. **Rozšiřitelnost** - Architektura umožňuje snadné přidání dalších místností, předmětů, NPC a příkazů v budoucnu.

---

## 6. Technické detaily

### Použité technologie

- **Jazyk:** C# (.NET)
- **Komunikace:** TCP/IP sockets
- **Ukládání dat:** JSON soubory
- **Architektura:** Server-Klient model

### Složení projektu

```
/workspace
├── onlineHra.sln              # Solution file
├── onlineHra/                 # Server projekt
│   ├── Program.cs             # Entry point serveru
│   ├── Networking/
│   │   ├── Server.cs          # TCP server logika
│   │   └── Player.cs          # Reprezentace hráče
│   ├── Services/
│   │   ├── WorldService.cs    # Správa herního světa
│   │   ├── PlayerService.cs   # Správa hráčů (registrace, login)
│   │   └── LoggingService.cs  # Logování událostí
│   ├── Models/
│   │   ├── PlayerState.cs     # Datový model hráče
│   │   ├── Room.cs            # Datový model místnosti
│   │   ├── Item.cs            # Datový modelu předmětu
│   │   └── Npc.cs             # Datový model NPC
│   ├── Commands/              # Implementace příkazů
│   │   ├── ICommand.cs        # Interface pro příkazy
│   │   ├── Go.cs, Explore.cs  # Pohyb
│   │   ├── Take.cs, Drop.cs, Inventory.cs  # Předměty
│   │   ├── Attack.cs          # Boj
│   │   ├── Trade.cs, Give.cs  # Quest mechaniky
│   │   ├── Say, Whisper, Broadcast, Talk  # Komunikace
│   │   └── Help, RegisterOrLogin.cs
│   └── Data/                  # JSON data světa
│       ├── rooms.json
│       ├── items.json
│       ├── npcs.json
│       └── players.json
│
└── onlineHraClient/           # Klient projekt
    ├── Program.cs             # TCP klient s UI
    └── onlineHraClient.csproj
```

---

*Datum vytvoření dokumentu: $(date +%Y-%m-%d)*  
*Autoři: Člověk S, Člověk L*
