
## SAMPLES-0001 — ponowne Close wewnątrz Closing, demo 05

2026-11-18, Roland Rusiecki: zrzut wyjątku WPF przy zamykaniu. Obsługa wyboru
Odrzuć oraz synchroniczne zakończenie Save mogły wywołać Close przed powrotem
z Closing. Agent przeniósł potwierdzenie i całą operację do dyspozytora po
anulowaniu pierwszego zamknięcia. Jeden przepływ, brak powtórnych dialogów.
Lokalnie, bez wydania/push. Weryfikacja w toku: LocalEditing.WpfChecks.

Weryfikacja SAMPLES-0001: rzeczywisty LocalEditing.WpfChecks PASS dla
Odrzuć, Zapisz i Anuluj (ostatni zachowuje okno i bufor). Potwierdzono brak
zapisu przy Odrzuć oraz zapis przy Zapisz. Próba zamknięcia jest początkowo
anulowana; ponowne Close następuje z dyspozytora po powrocie z Closing.
Bez commit/push/publikacji. Logi: Phiale.Shell/artifacts-temp/loading-preview.

## SAMPLES-0002 — stare zależności mimo lokalnych poprawek bibliotek

2026-11-18, Roland Rusiecki: zrzut aktywnego checkboxa bez poprawki i polecenie
uruchomienia poprawek we wszystkich demach. Przyczyna: osiem projektów miało
rozproszone, nadal opublikowane wersje Grid preview.11 i Design preview.48.
Agent ujednolicił wybór wersji dla VS, Debug/Release i testów. Lokalny, ignorowany
override wybiera nowe binarne paczki; świeży klon zachowuje nuget.org-only.
Grid jest chroniony istniejącym procesem obfuskacji; Design zgodnie z decyzją
autora pozostaje bez obfuskacji. Stan lokalny; bez commit/push/publikacji.
Weryfikacja w toku: wszystkie osiem aplikacji, faktyczne hashe DLL i edytor bool.

Weryfikacja SAMPLES-0002 zakończona lokalnie: osiem aplikacji Debug i Release
kompiluje się z 0 ostrzeżeń/0 błędów. Dla 16 katalogów wynikowych porównano
deps.json i SHA256 sześciu rzeczywistych DLL z nowymi paczkami: zgodne.
LocalEditing.WpfChecks na rzeczywistym demo potwierdza 20x20, jeden znacznik
i brak zewnętrznej obwódki, testuje siedem edytorów oraz zapis/odrzucenie.
Zapisz/Odrzuć/Anuluj przy zamykaniu PASS. Obraz 05-checkbox-edit.png obejrzany.
Testy wszystkich scenariuszy PASS: SQLite snapshot, 15 query, natywne przewijanie,
19 REST, 29 server grouping i natywne group-end, 46 local editing,
33 REST snapshot/conflicts/validation, 8 UserControl details, 10 nested-grid.
Ponadto 22 testy edytorów/podglądu/viewportu na faktycznie obfuskowanych NuGetach
PASS. Wyszukanie wersji jest wspólne dla rozwiązania, projektów i testów.
Poprawiono też pozostałe numery 04/05/06/07 wewnątrz nagłówków dem 05/06/07/08.
Paczki: Grid 2.0.0-preview.12-local.1, Design WPF 0.1.0-preview.49-local.1.
Brak publikacji NuGet i Git. Lokalny feed i override pozostają potrzebne;
świeży klon nadal wybiera publiczne paczki. Dowody:
E:/GitRepos/Phiale.Shell/artifacts-temp/demo-fixes-20261118 oraz
artifacts/local-feed/demo-binaries.json.

## Odbiór kontrolek i rzeczywistego przewijania — 2026-11-18

Roland Rusiecki zgłosił brak ikon i rozdzielenia operacji oraz brak domyślnej
karty usunięcia wewnątrz Grida. Agent podłączył nowy zestaw do wszystkich ośmiu
dem, bez publicznego wydania: Grid 2.0.0-preview.12-local.2 (obfuskowany),
Design WPF 0.1.0-preview.49-local.2 (bez obfuskacji). Ignorowany override
wskazuje artifacts/local-feed-v2. Zwykły klon nadal używa publicznych wersji.

Debug i Release: 0 błędów i 0 ostrzeżeń. W 16 katalogach wynikowych wersje
w deps.json i SHA256 sześciu bibliotek są zgodne z rzeczywistymi paczkami.
Wszystkie dziewięć programów kontrolnych PASS: SQLite snapshot, 15 Query,
natywne przewijanie/paging, 19 REST, 29 server grouping i natywne group-end,
46 local editing, natywne edytory i Save/Discard, 33 REST snapshot/conflicts,
8 UserControl details, 10 nested-grid. Osobno Close Save/Discard/Cancel PASS.

Demo 05: rzeczywisty przycisk Usuń otwiera kartę, Anuluj zachowuje rekord,
akceptacja trafia wyłącznie do bufora, przywrócenie czyści zmianę. Render pokazuje
ikony Design, dwie opisane grupy i kartę z ikonami. Checkbox ma pojedynczy,
powiększony znacznik. Demo 02: rzeczywisty scroll zachowuje poprzednie widoczne
wartości podczas odczytu; render obejrzany. Obrazy są pod scroll-preview-fix/demos.
Chronione paczki: 33 WPF i 15 Avalonia PASS. Dokumentacja użytego API:
292 symbole/119 plików/0 diagnostics, opisy PL/EN/DE/FR zgodne. Import strony
lokalnie oraz 142 testy PASS, bez wdrożenia. Tryb bezpośredniego zapisu nie jest
częścią tej poprawki. Otwarte alarmy wydajności są w dzienniku Grida.
Dowody: E:/GitRepos/Phiale.Shell/artifacts-temp/scroll-preview-fix;
artifacts/local-feed-v2/demo-binaries.json.

Korekta autora 2026-11-18: podpisy grup paska usunięte w Grid WPF/Avalonia.
Lokalne dema otrzymały chronione preview.12-local.3, Design pozostaje
preview.49-local.2. Osiem aplikacji Debug/Release: 0 błędów/ostrzeżeń,
deps/SHA256 zgodne. Natywne demo edycji PASS; render bez podpisów obejrzany.
Dowody: Phiale.Shell/artifacts-temp/compact-commandbar* i
artifacts/local-feed-v3/demo-binaries.json. Lokalnie; bez publikacji.

GRID-0021, 2026-11-18 — dema otrzymały lokalne chronione Grid preview.12-local.4.
Naprawiono ucinanie wbudowanego paska oraz nierówną wysokość akcji karty usunięcia.
Wszystkie osiem aplikacji Debug/Release: 0 błędów/ostrzeżeń; deps/SHA256 zgodne.
Natywne demo 05 PASS, render z równymi przyciskami i zawiniętym paskiem obejrzany.
Design preview.49-local.2 bez zmiany. WPF13/Avalonia19 testów paczek PASS.
Feed artifacts/local-feed-v4; dowody Phiale.Shell/artifacts-temp/toolbar-dialog*.
Zestaw lokalny, bez wydania na Git/NuGet.

Odbiór 2026-11-18 — Grid preview.12-local.6 / Design preview.49-local.2:
wszystkie osiem dem Debug/Release zgodne (deps/SHA256). Demo05 pokazuje Buffered
i Immediate z ukrytymi akcjami bufora; rzeczywisty zapis SQLite i walidacja PASS.
Demo08 ma trzy pola mastera i kontrolę sum powiązanych z rodzicem;12 kontroli PASS.
Pasek adaptacyjny bez ramek i czytelne usunięcie z pojedynczym Undo sprawdzone
w obu UI. Cały zestaw programów kontrolnych dem PASS. Opisy aplikacyjnego dialogu,
własnych komend VM i opcji menu są w dokumencie komponentu w czterech językach.
297 symboli API udokumentowanych;142 testy strony PASS. Wewnętrzny dziennik Grida
zachowuje dwa błędy schowka WPF i otwartą bramkę wydajności. Bez publikacji.
Feed artifacts/local-feed-v6; dowody Phiale.Shell/artifacts-temp/current-*.


2026-11-19: końcowy odbiór publicznego Grid preview.12 / Design preview.49. Wszystkie8dem Debug/Release,9runnerów i warianty Immediate/close-save/close-cancel PASS. Wszystkie16zestawów zależności i hashe DLL zgodne z opublikowanymi paczkami; lokalny override przeniesiony do ignorowanych artefaktów. Nie jest to jeszcze wynik świeżego klonu VM.
