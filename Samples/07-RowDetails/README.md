# 07 — własny UserControl wewnątrz wiersza

Cel: po kliknięciu + rozwinąć własną kartę szczegółów pod właściwym rekordem,
wewnątrz tabeli. To nie jest panel obok Grida ani osobne okno.

~~~powershell
dotnet run --project Samples/07-RowDetails/RowDetails.csproj -c Release
~~~

Korzystamy z 24 rekordów istniejącej Data/local-editing.sqlite z demo 04.
AssetCatalog otwiera plik read-only. Nie zmieniamy bazy roboczej z demo edycji.
Model AssetRow jest dołączony z demo 04; nie ma drugiej kopii definicji.

## Cztery elementy połączenia

1. **AssetDetailsProvider** mówi Core, czy rekord ma szczegóły. Zwraca neutralny
   opis: tożsamość wiersza, wysokość i dane AssetDetailsViewModel. Nie tworzy WPF.
2. **AssetDetailsFactory** jest małym adapterem WPF. Tworzy własny AssetDetailsView
   i przypisuje mu dane z opisu. Nie decyduje o tym, który rekord rozwinąć.
3. **AssetDetailsView.xaml** to zwykły UserControl aplikacji. Pokazuje cenę,
   użytkowanie, termin przeglądu i uwagi przez binding do własnego ViewModelu.
4. **MainWindow.xaml** przypisuje RowDetailProvider i RowDetailContentFactory.
   Grid obsługuje przycisk +, rozwijanie, przewijanie i umieszczenie kontrolki.

| Plik | Odpowiedzialność |
| --- | --- |
| Infrastructure/AssetCatalog.cs | Jeden jawny SELECT z małej bazy, wyłącznie odczyt |
| Composition/AssetGridComposition.cs | Lokalna sesja danych Grida, bez zapisu |
| ViewModels/AssetsViewModel.cs | Kontekst MVVM głównego Grida i kolumny |
| Details/AssetDetailsProvider.cs | Dane i neutralny opis szczegółów konkretnego wiersza |
| Details/AssetDetailsFactory.cs | Zamiana opisu na natywny UserControl |
| ViewModels/AssetDetailsViewModel.cs | Wartości prezentowane we własnej karcie |
| Views/AssetDetailsView.xaml | Własny wygląd zawartości karty |

Każda karta ma dane swojego wiersza. Nie wiążemy jej do Grid.CurrentItem:
zmiana bieżącej komórki nie może podmienić zawartości innej, nadal rozwiniętej karty.
Można rozwinąć kilka rekordów. Nie dokładamy pobierania asynchronicznego ani edycji,
ponieważ wszystkie dane tej karty są już w rekordzie. Fabryka nie zakłada zdarzeń
ani zasobów wymagających własnego sprzątania; zamknięcie widoku zwalnia runtime,
a kompozycja — sesję.

## Sprawdź samodzielnie

- Rozwiń EQ-001 i EQ-002, a potem zmień bieżący wiersz. Karty zachowują swoje dane.
- Dodaj Location do własnego UserControl; nie zmieniaj kodu biblioteki.
- Zwiń i ponownie rozwiń kartę, przewiń tabelę. Nie otwiera się osobne okno.

Kod przykładu i dane syntetyczne: MIT. Zależności zachowują własne licencje.
Paczki Grida: opublikowane 2.0.0-preview.11.
