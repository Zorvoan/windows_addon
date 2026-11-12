# Better Windows

Vlastní „shell“ nad Windows 11: horní lišta, dock a další prvky. Windows i všechny aplikace dál běží normálně, Better Windows jen přidává vlastní okna nad ně.

## Požadavky

- Windows 11 22H2 nebo novější (cíleně laděno na 25H2)
- [.NET 8 SDK](https://dotnet.microsoft.com/download) (novější SDK funguje taky)
- [Node.js](https://nodejs.org/) 22 LTS (Vite vyžaduje ≥ 20.19)
- Microsoft Edge WebView2 Runtime (ve Windows 11 je předinstalovaný)

## Spuštění (jeden příkaz)

```powershell
dotnet run --project src/BetterWindows
```

První spuštění samo nainstaluje npm balíčky a sestaví rozhraní v `ui/`. Další buildy sestaví rozhraní znovu jen tehdy, když se v `ui/` něco změnilo.

Jen sestavení:

```powershell
dotnet build
```

## Vývoj rozhraní

Rozhraní je Svelte 5 + TypeScript, sestavované přes Vite. Nenačítá se z vývojového serveru: WebView2 čte soubory přímo ze složky `ui` vedle exe přes virtuální doménu `https://app.betterwindows/`.

Rychlá smyčka při ladění UI:

1. Spusť aplikaci (`dotnet run --project src/BetterWindows`).
2. V druhém terminálu spusť `npm run watch` ve složce `ui/`. Vite při každé změně přestaví `ui/dist`.
3. V okně stiskni **Ctrl+R**. Debug build spuštěný z repozitáře čte rozhraní přímo z `ui/dist`, takže C# není potřeba znovu sestavovat.

V Debug buildu fungují v každém okně vývojářské nástroje (**F12**).

## Struktura

```
src/BetterWindows/        WPF aplikace (.NET 8) – Core, Native, Monitors, Services, ShellWindows, Web, Tray
ui/                       rozhraní (Svelte + TypeScript + Vite)
  src/lib/styles/tokens.css   designové tokeny: jediné místo pro barvy, blur, zaoblení, písmo
tools/make_icon.py        generátor vlastní ikony aplikace
docs/ARCHITECTURE.md      vrstvy oken, složky, zásady
docs/ROADMAP.md           plán kroků a dalších fází
```

## Data aplikace

| Co | Kde |
|---|---|
| Nastavení (od kroku 10) | `%AppData%\BetterWindows\settings.json` |
| Logy (7 dní) | `%AppData%\BetterWindows\logs\` |
| Cache WebView2 | `%LocalAppData%\BetterWindows\WebView2\` |
