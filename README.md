# PhialeTech.Grid.Samples.Wpf

Małe przykłady użycia Grida, zawsze w MVVM. Każdy projekt pokazuje jedno zagadnienie.

## 01 — SQLite → Model → ViewModel → Grid

Jeden zwykły Window WPF i jeden PhialeGrid. Bez Shella, master–detail, serwera REST, edycji ani dodatkowych paneli aplikacji.

```text
Data/warehouse-1000000.sqlite
  → SqliteStockRepository: SELECT i mapowanie wyniku
  → StockRow: model jednego stanu towaru w jednym magazynie
  → StocksViewModel.Rows: dane / Columns: kolumny z GridColumnsBuilder
  → StockGridComposition: CreateLocalSession(rows, readOnly: true)
  → MainWindow.xaml: DataSession="{Binding DataSession}" / Columns="{Binding Columns}"
```

## Uruchomienie

Windows, .NET 8 SDK i komponenty WPF. Aktualnie Grid jest jeszcze w repozytorium PhialeGis.Library. Przykład używa referencji projektowych do jego bibliotek, **nie do demo ani Shella**. Wymagany lokalny feed Phiale.Design z paczką WPF 0.1.0-preview.47. Nie przedstawiamy tych zależności jako publicznie dostępnych paczek.

Oczekiwany układ lokalny:

```text
E:/GitRepos/
  PhialeTech.Grid.Samples.Wpf/
  PhialeGis.Library/
  Phiale.Design/artifacts/packages/Phiale.Design.Wpf.0.1.0-preview.47.nupkg
```

```powershell
cd E:\GitRepos\PhialeTech.Grid.Samples.Wpf
dotnet run --project Samples/01-SqliteBinding/SqliteBinding.csproj -c Release
```

Visual Studio: otwórz `PhialeTech.Grid.Samples.Wpf.sln`, ustaw `SqliteBinding` jako projekt startowy. Żadne argumenty startowe nie są potrzebne. Zmienną MSBuild `PhialeGridRoot` można ustawić, jeśli źródła Grida leżą gdzie indziej; lokalizację feeda Design zmienia się w `NuGet.Config`.

## Co gdzie jest

| Plik w Samples/01-SqliteBinding | Odpowiedzialność |
| --- | --- |
| `Models/StockRow.cs` | Czysty model rekordu; `Available = OnHand - Reserved` |
| `Models/IStockRepository.cs` | Kontrakt odczytu niezależny od SQLite i WPF |
| `Infrastructure/SqliteStockRepository.cs` | Połączenie read-only, JOIN i zamiana kolumn SQL na właściwości modelu |
| `ViewModels/StocksViewModel.cs` | Builder kolumn, kolekcja Rows, stan ładowania i INotifyPropertyChanged |
| `Views/MainWindow.xaml` | Widok z dwoma bindingami do Grida |
| `Views/MainWindow.xaml.cs` | Wyłącznie podłączenie API oraz zdarzenia życia widoku i anulowania |
| `App.xaml.cs` | Kompozycja repozytorium, ViewModelu i runtime Grida; własność sesji do zamknięcia aplikacji |
| `Composition/StockGridComposition.cs` | Tworzy lokalną sesję read-only istniejącego Grida i neutralny binding; nie implementuje pagingu |
| `App.xaml` | Import gotowych zasobów Phiale.Design i Grid Styles; brak lokalnych stylów |

## Gdzie model łączy się z SQLite

W `SqliteStockRepository.ReadAll` aliasy SQL opisują pola modelu:

```sql
SELECT b.warehouse_id || ':' || b.product_id AS Id,
       p.code AS ProductCode, p.name AS ProductName, p.unit AS UnitCode,
       w.name AS WarehouseName, b.on_hand AS OnHand, b.reserved AS Reserved
FROM stock_balances AS b
JOIN products AS p ON p.id = b.product_id
JOIN warehouses AS w ON w.id = b.warehouse_id
ORDER BY p.code, w.id
```

`new StockRow(ProductCode: reader.GetString(1), ...)` jest właściwym mapowaniem SQL → model. Alias sam nie tworzy bindingu. Kolejność odczytu odpowiada kolejności kolumn SELECT. Ilości są zapisane jako tekst dziesiętny i odczytywane przez `decimal.Parse(..., CultureInfo.InvariantCulture)`.

## Jak skonstruowany jest ViewModel

Konstruktor otrzymuje `IStockRepository` oraz fabrykę neutralnego `GridDataBinding` z kompozycji aplikacji. ViewModel nie zna ścieżki pliku, SQL ani kontrolki WPF. Udostępnia `Columns`, `Rows`, `DataSession` i `Status`.

```csharp
public IReadOnlyList<IGridColumnDescriptor> Columns { get; } = new GridColumnsBuilder<StockRow>()
    .Add(row => row.ProductCode, "Symbol", 130)
    .Add(row => row.ProductName, "Product", 310)
    .Add(row => row.WarehouseName, "Warehouse", 200)
    .Add(row => row.UnitCode, "Unit", 65)
    .Add(row => row.OnHand, "On hand", 115)
    .Add(row => row.Reserved, "Reserved", 115)
    .Add(row => row.Available, "Available", 115)
    .Build();
```

`GridColumnsBuilder<TRow>` należy do `PhialeGrid.Contracts`, nie do przykładu ani WPF. Lambda `row => row.ProductCode` daje Id kolumny `ProductCode` i typ właściwości. Grid odczytuje potem tę właściwość z każdego obiektu w Rows. Builder przyjmuje bezpośrednią publiczną właściwość; obliczenia umieszcza się w modelu, np. `Available`.

`LoadAsync` pobiera modele z repozytorium i przypisuje je do `Rows`, po czym zgłasza `PropertyChanged`. Fabryka opakowuje Rows w lokalną sesję Core Grida; WPF otrzymuje `DataSession` przez binding. Nie łączymy jednocześnie ItemsSource i DataSession. Odczyt SQLite działa w `Task.Run`; kontynuacja ViewModelu wywołana z Loaded wraca na kontekst UI.

```xml
<grid:PhialeGrid Columns="{Binding Columns}"
                DataSession="{Binding DataSession}"
                IsPagingEnabled="True"
                DataPageSize="100"
                IsGridReadOnly="True" />
```

Przykład pokazuje **pełny odczyt do pamięci**, nie stronicowanie SQL, data virtualization ani tryb serwerowy. Sortowanie i filtrowanie pracują na załadowanych obiektach. 100 000 rekordów pozwala obserwować ten wariant, ale nie stanowi obietnicy braku przycięć dla dowolnego zbioru. Do większych źródeł przygotujemy osobny przykład, bez komplikowania tego projektu.

## Dołączona baza

Plik `Data/warehouse-100000.sqlite` jest rzeczywistą, dołączoną bazą (9 056 256 bajtów). Nie jest generowany przy starcie. MSBuild kopiuje go do `bin/.../Data`. Połączenie jest read-only; brak pliku powoduje komunikat, a nie utworzenie pustej bazy.

- `products`: 25 000 syntetycznych, nazwanych towarów przemysłowych.
- `warehouses`: 4 magazyny.
- `stock_balances`: 100 000 stanów, po jednym na parę magazyn/towar.
- SHA-256: `5e09fb7aa110431fb2bbef3b8b59c3ae73e0df5eb51c8e6c58fdc09bb8901fc6`.

Zestaw `industrial-stock-v1` pochodzi z demonstratora autora Phiale.Shell. Są to dane syntetyczne, nie dane klientów ani prawdziwe stany przedsiębiorstwa. Schemat i liczności: `Data/warehouse-100000.json`.

## Weryfikacja

```powershell
dotnet run --project Tests/SqliteBinding.Checks -c Release
```

26 kontroli: odczyt 100k, unikatowe ID, towary/magazyny, ilości decimal, nazwy, powiadomienia ViewModelu, mapowanie i typy kolumn, anulowanie, brak tworzenia brakującej bazy i odrzucenie zagnieżdżonego selektora. Sprawdzono także rzeczywiste okno WPF. Nie jest to benchmark całej aplikacji.

## Ustalenia i wykonanie — 2026-11-13

Autor wymagał zawsze MVVM, jednego prostego przykładu, kolumn z buildera, dużej bazy SQLite dołączonej do repozytorium i objaśnienia na stronie WWW. Następnie polecił umieścić builder w bibliotece Grida i ujednolicić menu Elements/Filter. Agent opracował implementację i opis, używając istniejących kontraktów autora. Zmiany pozostają lokalne — bez commitów i pushów.

## Visual Studio i nowy builder — 2026-11-13

Po dodaniu GridColumnsBuilder trzeba przebudować także PhialeGrid.Contracts w konfiguracji Debug. Rozwiązanie zawiera teraz projekty bibliotek w folderze Grid, dzięki czemu VS zna zależności i kolejność budowania. Directory.Solution.props wyrównuje wersję Design w kompilacji całego rozwiązania. Jeśli rozwiązanie było już otwarte, zaakceptuj Reload po zmianie .sln, następnie Build Solution. Nie trzeba ręcznie kopiować DLL.

## Konfiguracja zamkniętego zbioru — 2026-11-13

Autor polecił pokazać operacje dostępne na załadowanych 100 000 rekordów oraz zablokować funkcje wymagające aktywnego źródła. Później polecił udostępnić lokalny paging — ta korekta zastępuje wcześniejszą blokadę regionu Paging. Agent dodał `Configuration/ReadOnlySnapshotPolicy.cs`; ViewModel udostępnia tę politykę przez `Capabilities`, a View wiąże `CapabilityPolicy="{Binding Capabilities}"`.

- `IsGridReadOnly=True` blokuje edycję. Kolumny buildera również mają `IsEditable=false`; kolekcja jest tylko do odczytu.
- Polityka udostępnia grupowanie, podsumowania i projektant podsumowań. Filtr i sortowanie operują na `Rows` w pamięci.
- Pasek poleceń edycji, zmiany i walidacja edycji są niedostępne. Paging jest dostępny na zamkniętej kolekcji. Jest lokalna `DataSession`; brak zapisu i kolejnych odczytów SQLite. Refresh lokalnej sesji oznacza ponowną projekcję kolekcji, nie odświeżenie bazy. `CanExecuteCommand` nie jest samodzielną ochroną edycji — tę zapewnia kontekst read-only.
- SQL wykonuje się raz podczas `LoadAsync`. Obsługa prezentacji nie wysyła kolejnych zapytań.
- Nowe cztery kontrole sprawdzają odmowę regionów, dostępność analizy lokalnej, odmowę poleceń edycji i brak sesji przed podłączeniem danych. Dalsze kontrole sprawdzają 100 rekordów na stronie, przejście do następnej strony, zmianę wielkości na 25, filtrowanie całego zbioru i brak dodatkowych odczytów repozytorium.

### Ćwiczenia

1. Prawy przycisk na `Warehouse` → `Group by this column`; rozwiń grupę.
2. `☰ → Elements → Filter`, wpisz `KAR-00001` pod `Symbol`, a następnie wyczyść filtr.
3. `Elements → Summary designer`, wybierz kolumnę i operację, `Add summary`; włącz `Summaries`. Nie sumuj biznesowo różnych jednostek — grupuj po `Unit` lub użyj `Count`.
4. Obserwuj przewijanie, sortowanie i reakcję na powyższe polecenia. Oddziel pierwszy start od rozgrzanej aplikacji; nie wyciągaj wniosków o SQL, REST ani milionie rekordów z tego jednego przykładu.

Filmy oraz powiązane opisy usunięto na polecenie autora. Nie dodano linków pobierania ani GitHub.

## Lokalny paging — korekta autora z 2026-11-13

Paging działa od startu, domyślnie po 100 rekordów. Przełącznik `☰ → Elements → Paging` włącza i wyłącza stronicowanie. Po odznaczeniu dostępny jest pełny wynik; po ponownym zaznaczeniu wracają strony z zachowanym rozmiarem. Wielkość strony zmienia się na pasku; dostępne są 10, 25, 50, 100 i 200. Przy 100 000 rekordów oraz 100 na stronę Grid pokazuje 1000 stron.

Cały zbiór nadal jest odczytywany raz do pamięci. Sortowanie, filtr i grupowanie obejmują lokalną kolekcję; sesja Grida wyznacza wynikowe strony. Nie dopisywano pagingu w ViewModelu ani adaptera SQLite. Fabryka `StockGridComposition.CreateBinding` tworzy jedną sesję na okno i zamyka ją w `App.OnExit`, po odłączeniu widoku. Przykład zachowuje MVVM i stylowanie wyłącznie z istniejących bibliotek.

Weryfikacja: 26/26 kontrole konsolowe, Release 0 błędów/ostrzeżeń. W prawdziwym WPF obejrzano pierwszą stronę (1–100 / 100000), kliknięcie Next (101–200) i listę rozmiarów strony. Zmianę na 25 oraz filtr całego zbioru potwierdzają testy API. Filmy nie są częścią aktualnego przykładu.

Korekta mechanizmu Grid (decyzja autora z tej samej rozmowy): przełącznik nie jest obejściem w demo. Reguła i pełne zapytanie należą do Core, WPF odtwarza tryb z API. Końcowy odbiór: 26/26 kontroli przykładu, 767/767 testów Core i 3/3 testy akceptacyjne WPF; Debug/Release kompilują się bez błędów i ostrzeżeń. Strona nie zawiera filmów. Bieżące okna uruchomione przed przebudową trzeba ponownie otworzyć, aby używały nowych DLL.

## Gotowy panel opcji Grida — 2026-11-13

Na polecenie autora krótki przykład podłącza PhialeToolsPanel przez PhialeGrid.SideToolContent w Views/MainWindow.xaml. Jest to gotowa kontrolka biblioteki Grida, nie zawartość implementowana przez aplikację. Menu ☰ → Elements → Grid options jest aktywne i otwiera ustawienia. Brak lokalnego stylowania i dodatkowej logiki w ViewModelu. Sprawdzono rzeczywiste WPF z 100 000 rekordów; 26 kontroli read-only i pagingu nadal zaliczonych. Bez commit/push.


## Dataset size comparison

The sample now includes `Data/warehouse-1000000.sqlite`: 250,000 product
variants across four warehouses, giving 1,000,000 stock balances. The original
100,000-row database is retained. Category identifies fasteners, packaging,
workwear and cables; group by Category and then Warehouse to compare workloads.
Additional variants are synthetic supplier variants, not real commercial stock.

Select 10,000, 100,000 or 1,000,000 and click **Load snapshot**. SQL applies
LIMIT before materialization. The default remains 100,000. Each load replaces
the read-only in-memory snapshot; local filtering, sorting and paging then use
that snapshot. The status displays load duration (database read, model/session
creation and synchronous binding notification), not a precise render benchmark.
Keep the same grouping, filter and paging configuration when comparing sizes.

The generator is `Tools/GenerateMillion.py`; it refuses to overwrite an existing
million-row database. The supplied file exceeds GitHub's ordinary 100 MiB limit;
use Git LFS or a release asset before publishing it. No publication is performed.

Author request: 2026-11-14, this conversation. Dataset expansion, category and
size selector implemented by the agent. Validation: `Tests/SqliteBinding.Checks`
checks all three SQL limits, quantities, categories and snapshot replacement.

## Tryb bez pagingu — 2026-11-15
MainWindow.xaml włącza EnableDataVirtualization. Po odznaczeniu Paging scrollbar obejmuje cały wynik, a Grid przygotowuje tylko potrzebne okna. Dane SQLite pozostają w pamięci adaptera; nie jest to paging SQL. Kopiowanie/eksport całego przefiltrowanego wyniku pozostają dostępne. Decyzję zatwierdził autor w rozmowie; implementacja i testy: agent.

## Baza danych i Git LFS

Plik `Data/warehouse-1000000.sqlite` jest przechowywany przez Git LFS, ponieważ przekracza limit zwykłego pliku GitHub. Po sklonowaniu repozytorium, z zainstalowanym Git LFS, wykonaj `git lfs pull`, aby pobrać pełną bazę zamiast wskaźnika LFS.
