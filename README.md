# MUD Online Hra

Textová multiplayerová hra (MUD) postavená na TCP/IP komunikaci mezi serverem a klientem.

## Požadavky

- **.NET 9.0 SDK** - [Stáhnout zde](https://dotnet.microsoft.com/download/dotnet/9.0)

## Struktura projektu

```
/workspace
├── onlineHra/          # Serverová aplikace
├── onlineHraClient/    # Klientská aplikace
└── NAVRH_DOKUMENTACE.md
```

## Jak projekt spustit

### 1. Otevření terminálu

Otevřete příkazový řádek (terminal) a přejděte do složky s projektem:

```bash
cd /workspace
```

### 2. Spuštění serveru

Server musí být spuštěn **nejprve**, než se mohou připojit klienti.

```bash
cd onlineHra
dotnet run
```

Server se spustí na portu **65525** a bude čekat na připojení klientů.

> **Poznámka:** Server nechte běžet na pozadí. Neukončujte ho, dokud nechcete ukončit herní relaci.

### 3. Spuštění klienta

Otevřete **nový terminál** (vedle toho se spuštěným serverem) a spusťte klienta:

```bash
cd /workspace/onlineHraClient
dotnet run
```

Klient se vás zeptá na:
- **Server address:** Zadejte `localhost` (pokud běží server na stejném počítači)
- **Server port:** Zadejte `65525` (nebo stiskněte Enter pro výchozí hodnotu)

### 4. Připojení dalšího hráče

Pro více hráčů otevřete další terminály a opakujte krok 3. Každý hráč potřebuje vlastního klienta.

## Základní ovládání

Po připojení se zaregistrujte nebo přihlaste:

```
login        - Přihlášení existujícího hráče
register     - Registrace nového hráče
```

### Herní příkazy

| Příkaz | Popis |
|--------|-------|
| `go <směr>` | Pohyb (north, south, east, west, up, down) |
| `explore` | Prozkoumání okolí |
| `take <předmět>` | Vzetí předmětu |
| `drop <předmět>` | Odložení předmětu |
| `inventory` | Zobrazení inventáře |
| `attack <nepřítel>` | Útok na NPC |
| `trade <předmět>` | Obchod s NPC |
| `give <předmět>` | Předání předmětu (quest) |
| `say <text>` | Zpráva všem v místnosti |
| `whisper <text>` | Soukromá zpráva |
| `broadcast <text>` | Zpráva všem hráčům |
| `talk <npc>` | Promluva s NPC |
| `help` | Zobrazení nápovědy |
| `quit` / `exit` | Odpojení od serveru |

## Příklad hraní

```
=== PŘIHLÁŠENÍ ===
Server: Do you want to login or register?
Klient: register
Server: Enter username:
Klient: player1
Server: Enter password:
Klient: *****
Server: Registration successful!

=== HRANÍ ===
Klient: help
Klient: go north
Klient: take sharp_sword
Klient: attack dragon
Klient: trade fake_egg
Klient: give golden_egg
```

## Cíl hry

Cílem je získat dva artefakty (**Golden Egg** a **Dragon Scale**) a předat je High Priestovi v chrámu:

1. Najděte **Sharp Sword** v armory
2. Najděte **Fake Egg** ve vault
3. Obchodujte s drakem: `trade fake_egg` → získáte **Golden Egg**
4. Porazte Dark Knighta: `attack dark knight` → získáte **Dragon Scale**
5. Předajte oba artefakty: `give golden_egg` a `give dragon_scale`

## Řešení problémů

### "Connection refused"
- Ujistěte se, že server běží před spuštěním klienta
- Zkontrolujte, zda je server na portu 65525

### Port již používán
- Pokud server nelze spustit, pravděpodobně již běží jiná instance
- Ukončete předchozí instanci serveru

### Klient se nepřipojuje
- Zkontrolujte, zda zadáváte správnou adresu (`localhost`) a port (`65525`)
- Pokud běží server na jiném počítači, použijte jeho IP adresu

## Autoři

- **Slavomír** - Server & Backend
- **Lukáš** - Klient & Komunikační protokol

Více informací viz [NAVRH_DOKUMENTACE.md](NAVRH_DOKUMENTACE.md).
