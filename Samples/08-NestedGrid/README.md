# 08 — tabela wewnątrz tabeli

Cel: rozwinąć wiersz lokalizacji i zobaczyć pełny PhialeGrid z jej wyposażeniem.
W odróżnieniu od demo 06 nie piszemy formularza UserControl ani własnej tabeli.

~~~powershell
dotnet run --project Samples/08-NestedGrid/NestedGrid.csproj -c Release
~~~

Ta sama mała baza z demo 04/06: 24 rekordy, sześć lokalizacji po cztery pozycje.
AssetRow z demo 04 i czytnik AssetCatalog z demo 06 są dołączone jako pliki źródłowe.
Aplikacje pozostają osobnymi projektami; nie uruchamiają się wzajemnie.

## Po kolei

1. LocationGridComposition grupuje dane raz, przed pokazaniem Grida. LocationRow
   zawiera stabilny Id lokalizacji i jej Assets. Otwieranie wiersza nie odpytuje bazy.
2. AssetRelation opisuje relację builderem GridDetailRelation.For<LocationRow, AssetRow>.
3. ParentKey i ChildKey określają ID. Local wskazuje gotową listę dzieci danego
   rodzica. Columns określa kolumny tej tabeli. Dispatcher przekazuje wywołania
   na wątek UI; Height ustala wysokość rozwiniętego obszaru.
4. MainWindow wywołuje BindDetailGrid. WPF tworzy pełną kontrolkę PhialeGrid;
   Core zarządza zagnieżdżonym runtime, źródłem i rozwijaniem. Nie tworzymy
   ręcznie drugiego silnika tabeli ani własnej kontroli stronicowania.

| Plik | Dlaczego istnieje |
| --- | --- |
| Models/LocationRow.cs | Jeden rodzic i lista należących do niego rekordów |
| Composition/LocationGridComposition.cs | Jednorazowe przygotowanie relacji i głównej sesji |
| Configuration/AssetRelation.cs | Cała deklaracja relacji: klucze, dane dzieci, kolumny, dispatcher, wysokość |
| ViewModels/LocationsViewModel.cs | Kontekst MVVM i kolumny głównej tabeli |
| Views/MainWindow.xaml.cs | Natywne podłączenie relacji i opcji widoku dziecka |

Filtr, zaznaczenie i przewijanie jednego dziecka nie zmieniają danych drugiego
ani rodzica. Domyślnie wspólne są metadane układu tej samej relacji, np. szerokości
kolumn. Nie oznacza to wspólnego filtra. Ten przykład jest lokalny i tylko do odczytu;
nie udaje zapisu ani transakcji całej rodziny. Zagnieżdżone API i jego lifetime
obsługuje Core/host — aplikacja nie wywołuje Dispose na pożyczonym API dziecka.

## Sprawdź samodzielnie

- Rozwiń dwie lokalizacje. Każda ma inne cztery rekordy.
- Ustaw filtr Elektronika w pierwszej tabeli. Druga zachowuje cztery rekordy.
- Dodaj kolumnę NextInspection w AssetRelation lub zmień Height(300).

Kod przykładu i dane syntetyczne: MIT. Zależności zachowują własne licencje.
Paczki Grida: opublikowane 2.0.0-preview.11.
