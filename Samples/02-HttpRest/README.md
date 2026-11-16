# 02 — Ta sama baza magazynowa przez HTTP REST

## Cel i szybkie uruchomienie

Pobierz z serwera drugą stronę po 100 rekordów. Grid i kolumny znasz z 01; nowy temat to żądanie i odpowiedź HTTP.

Z katalogu głównego repozytorium, po przywróceniu zależności i danych z Git LFS.
Serwer i klient uruchom w dwóch osobnych terminalach.

```powershell
dotnet run --project Samples/02-HttpRest/Server/HttpRest.Server.csproj -c Release
dotnet run --project Samples/02-HttpRest/Client/HttpRest.csproj -c Release
```

## Prześledź jeden przypadek

```text
Grid → CreateServerRequest → HTTP → EF Core / SQLite → ReadServerResponse → Grid
```

1. **StocksEndpoint.cs / CreateServerRequest** — Grid podaje Offset = 100 i Size = 100. Wysyłamy skip = 100 i take = 100. To tłumaczenie formatu; serwer nie zna typów Grida.
2. **EfStockRepository.cs** — SQLite liczy wszystkie pasujące rekordy i wybiera tylko żądaną stronę. Serwer zwraca stocks oraz total.
3. **StocksEndpoint.cs / ReadServerResponse** — stocks staje się Items, total staje się TotalCount. Dla pełnej bazy druga strona ma 100 rekordów, a TotalCount nadal wynosi 1 000 000.

## Sprawdź, czy rozumiesz

- Zmień DataPageSize ze 100 na 25 w MainWindow.xaml. Porównaj take w logu serwera.
- Wskaż miejsce zmiany, gdy inne API oczekuje start zamiast skip. Wyjaśnij, dlaczego TotalCount nie może być długością bieżącej strony.

Poniżej znajdują się pełne informacje o plikach, konfiguracji i ograniczeniach.


Przykład 01 odczytuje SQLite w aplikacji WPF. Przykład 02 odczytuje **tę samą
bazę Data/warehouse-1000000.sqlite** na serwerze ASP.NET Core. Klient WPF pobiera
przez HTTP tylko potrzebną stronę. Serwer nie zależy od bibliotek Phiale.

Nie tworzymy nowej bazy, nie kopiujemy miliona rekordów do innego pliku
i nie generujemy dodatkowych produktów. Istniejąca baza jest otwierana read-only.

## Uruchomienie

Windows, SDK .NET 8 lub nowszy i Windows Desktop Runtime .NET 8.
Zależności pochodzą z NuGet.org. Z katalogu głównego repozytorium:

```powershell
git lfs install
git lfs pull
# Terminal 1 — zostaw serwer uruchomiony
dotnet run --project Samples/02-HttpRest/Server/HttpRest.Server.csproj -c Release
# Terminal 2 — klient WPF
dotnet run --project Samples/02-HttpRest/Client/HttpRest.csproj -c Release
```

Serwer wypisze ścieżkę istniejącej bazy i adres http://127.0.0.1:5187.
Brak bazy kończy uruchomienie czytelnym błędem. Serwer niczego nie tworzy.
W Visual Studio uruchom HttpRest.Server i HttpRest.Client jako dwa projekty startowe.

## EF Core — wariant domyślny

W Visual Studio wybierz profil **EF Core** projektu HttpRest.Server.
Komenda uruchomienia serwera powyżej również używa EF Core.

- WarehouseDbContext.cs mapuje istniejące products, warehouses i stock_balances.
- EfStockRepository.cs buduje IQueryable, wykonuje CountAsync oraz
  OrderBy / Skip / Take / ToListAsync. Dopiero pobrana strona jest mapowana na StockRow.
- StockFilters.cs pokazuje dozwolone pola i typowane warunki, np. text => text == value.
- StockSorting.cs pokazuje typowane pola i zwykłe OrderBy / ThenBy.
- PredicateComposer.cs jest małym łącznikiem predykatów AND/OR. Reguły są w StockFilters;
  ten plik techniczny pozwala wykonać je razem w jednym zapytaniu SQL.
- StockReadRow.cs to wewnętrzny model projekcji z bazy, z zachowaniem źródłowych ilości.
- StockQuery.Validate sprawdza wejście identycznie dla obu implementacji.

Każdy odczyt ma osobny DbContext z wyłączonym śledzeniem. COUNT i strona
korzystają z jednej transakcji odczytu. Nie ma migracji, tworzenia ani zasilania bazy.
Quantity jest mapowaniem EF do CAST AS REAL: istniejące ilości są zapisane jako TEXT.
Wariant EF stosuje tę samą semantykę SQLite lower co SQL, także dla parametrów.
Ładowanie strony zachowuje oryginalne wartości ilości jako decimal.
Klient ma timeout 60 sekund: odczyt końcowych stron i nieindeksowane sortowania
miliona rekordów mogą trwać dłużej niż 10 sekund. To nie optymalizacja zapytań.

EF Core jest zależnością **serwera demo**, nie Grida ani klienta WPF.
Wersja: Microsoft.EntityFrameworkCore.Sqlite.Core 8.0.34; aplikacja nadal net8.0.
Konsola pokazuje SQL generowany przez EF; wartości parametrów pozostają ukryte.

## Porównanie z bezpośrednim SQL

Najpierw zatrzymaj poprzedni serwer. W Visual Studio wybierz profil **SQL comparison**
albo dopisz -- --sql do komendy dotnet run serwera. Oba warianty używają tego samego
portu 5187 i endpointu /api/stocks/search. Klient WPF nie wymaga zmiany.

Dokumentacja EF: https://learn.microsoft.com/ef/core/providers/sqlite/ oraz
https://learn.microsoft.com/ef/core/querying/async . Implementację opracowano
samodzielnie dla kontraktu tego przykładu; dokumentacja służy weryfikacji API.

## Sprawdź

1. Grid pokazuje 100 z 1 000 000 stanów magazynowych.
2. Kliknij On hand — ilości sortuje SQL na serwerze.
3. Filtr Category = Cables — rekordy i licznik odpowiadają filtrowi.
4. Przejdź na kolejną stronę — następne rekordy pobierane są przez HTTP.
5. Terminal serwera pokazuje skip, take oraz total.

Bez serwera występuje błąd odczytu. Nie ma lokalnego zbioru zastępczego.
Ctrl+C zatrzymuje serwer.

## Pliki i odpowiedzialności

| Plik | Rola |
| --- | --- |
| ../01-SqliteBinding/Models/StockRow.cs | Ten sam model magazynowy; dołączony do obu projektów jako plik, bez zależności od aplikacji WPF. |
| Client/ViewModels/StocksViewModel.cs | Te same kolumny magazynowe, binding sesji. |
| Client/Infrastructure/StocksEndpoint.cs | Mapuje format Grida na kontrakt REST i odpowiedź API na wynik Grida. |
| Client/Composition/StockGridComposition.cs | HttpClient, CreateHttpSession i zwalnianie zasobów. |
| Client/Views/MainWindow.xaml | Columns i DataSession, bez wykonywania HTTP w widoku. |
| Client/Configuration/RestExamplePolicy.cs | Udostępnia tylko funkcje obsługiwane przez API. |
| Server/StockQuery.cs | Własny kontrakt wejściowy serwera, bez typów Grida. |
| Server/StockRepository.cs | SELECT z istniejących stock_balances, products i warehouses; parametry SQL. |
| Server/StockServer.cs | Endpoint REST i odnalezienie istniejącego pliku bazy w repozytorium. |
| Server/Program.cs | Uruchamia ASP.NET Core. |

```text
Grid → Data.Http → StocksEndpoint → POST /api/stocks/search
                                       ↓
                       StockRepository → istniejąca baza magazynowa
                                       ↓
                         JSON: stocks + total → Grid
```

## Kontrakt serwera

```json
{
  "skip": 0,
  "take": 100,
  "orderBy": [{ "field": "onHand", "direction": "desc" }],
  "match": "all",
  "conditions": [{ "field": "category", "comparison": "eq", "value": "Cables" }]
}
```

Odpowiedź: stocks — bieżąca strona, total — liczba wszystkich pasujących stanów.
Klient tłumaczy Offset/Size, Sorts i FilterGroup na powyższy format.
Serwer nie otrzymuje GridQueryRequest ani tekstu SQL od klienta.

Pola: id, productCode, productName, category, warehouseName, unitCode, onHand,
reserved, available. Warunki: eq, contains, startsWith, gt, lt, between
(value oraz upper); operacje tekstowe dotyczą tekstu. match: all = AND, any = OR.
Strona 1–100 rekordów, do 8 sortowań i 8 warunków.

WHERE, COUNT, ORDER BY i LIMIT/OFFSET wykonuje SQLite. COUNT i strona pochodzą
z jednej transakcji odczytu. Nazwy pól pochodzą z zamkniętej listy, wartości są
parametrami. Dodatkowe klucze produktu i magazynu stabilizują kolejność między stronami.
Ilości w istniejącym schemacie są tekstem; porównania i sortowanie liczbowe
korzystają z CAST AS REAL, a model odczytuje źródłowe wartości jako decimal.
Available jest różnicą OnHand i Reserved; SQL zaokrągla ją do dwóch miejsc
zgodnie z precyzją ilości tego zbioru. Ten przykład nie zmienia schematu ani indeksów.

## Sam serwer — bez Grida

```powershell
$body = @{ skip = 0; take = 2 } | ConvertTo-Json
Invoke-RestMethod http://127.0.0.1:5187/api/stocks/search -Method Post -ContentType application/json -Body $body
```

## Bearer i zakres przykładu

Jeżeli Twoje API wymaga tokenu, aplikacja uzyskuje go podczas logowania
i ustawia w StockGridComposition przed CreateHttpSession:

```csharp
_http.DefaultRequestHeaders.Authorization =
    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
```

Tokenu nie zapisuj w kodzie. Jego odświeżanie obsługuje aplikacja, np.
DelegatingHandler. Serwer musi sprawdzać token i uprawnienia niezależnie od klienta.
Lokalny przykład nie zawiera logowania, HTTPS produkcyjnego, zapisu,
grupowania ani podsumowań. SQLite lower obsługuje zmianę wielkości liter ASCII.
Zapytania na milionie rekordów mogą skanować dane i sortować wynik — to nie
gwarancja konkretnej wydajności. Optymalizacje należy dobierać według pomiarów.

## Testy i pochodzenie

```powershell
dotnet run --project Tests/HttpRest.Checks -c Release
```

Testy używają istniejącej milionowej bazy read-only, porównują wyniki HTTP
z bezpośrednim SQL i sprawdzają SHA256 pliku przed oraz po wykonaniu.
Serwer testowy jest zatrzymywany automatycznie.

StocksEndpoint używa MapQueryRequest i ReadQueryResponse do jawnego dopasowania
zapytania Grida oraz odpowiedzi serwera do kontraktu tego API.

2026-11-16: autor wymagał prostego REST z normalnym serwerem odpytującym bazę,
następnie skorygował wybór danych: wykorzystać istniejącą bazę z pierwszego demo.
Agent usunął własny generator produktów, podłączył magazyn read-only i wspólny
model StockRow, dostosował klienta, testy i lokalną instrukcję.
Kod przykładu MIT; zależności zachowują własne licencje.
Zmiany lokalne, bez commitowania, push i publikacji.
2026-11-16: autor zlecił sprawdzenie serwera REST z EF. Agent dodał wariant EF Core jako domyślny, zachował SQL do porównań oraz przygotował testy zgodności na istniejącej bazie i dokumentację przy komponencie Grid. Bez publikacji i commita.

Weryfikacja wariantu EF 2026-11-16: 19 kontroli adaptera HTTP i 31 porównań EF/SQL — PASS.
Baza milionowa pozostała niezmieniona (SHA256 przed/po). Debug serwera i Debug/Release
klienta: 0 błędów, 0 ostrzeżeń. Testy obejmują COUNT i LIMIT/OFFSET wykonywane przez SQLite.

2026-11-16: autor wymagał większej czytelności przykładu edukacyjnego. Agent zastąpił refleksję w filtrach i sortowaniu typowanymi lambdami, wydzielił niewielkie łączenie predykatów i pokazał krok po kroku filtrowanie, liczenie oraz pobranie strony. Kontrakt REST, obsługiwane operatory i baza pozostały takie same.

Po uproszczeniu: 19 kontroli HTTP oraz 31 porównań EF/SQL PASS, w tym mieszane AND/OR oraz dodatkowe pola sortowania. Debug/Release serwera: 0 błędów, 0 ostrzeżeń. Baza bez zmian.

2026-11-16 — MVVM: ViewModel udostępnia GridContext<T> jako Grid; XAML wiąże Context. CurrentRecordId i SelectedRecordIds można odczytać w ViewModelu. Paczki preview.10 zostały opublikowane na NuGet.org 2026-11-17. Kompozycja zachowuje własność sesji. Kontekst należy odłączyć przed zamknięciem widoku.
