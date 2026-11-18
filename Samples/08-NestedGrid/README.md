# 08 — edytowalny master i detail

Cel: edytować dane lokalizacji w tabeli głównej i wyposażenie w rozwiniętej tabeli.
Master ma cztery pola: lokalizację (stały klucz), osobę kontaktową, telefon i uwagi.
Detail ma kod, nazwę, kategorię, ilość i cenę. Edytowalne są nazwa, ilość i cena.

```powershell
dotnet run --project Samples/08-NestedGrid/NestedGrid.csproj -c Release
```

## Co robi użytkownik

1. W masterze dwuklik na kontakcie, telefonie lub uwagach rozpoczyna edycję.
2. Enter/Zastosuj kończy edycję i przenosi zmianę do bufora **tej tabeli**.
3. Jej Zapisz utrwala bufor w SQLite; Odrzuć zmiany przywraca stan sprzed edycji.
4. Rozwiń lokalizację przyciskiem +. Zmień nazwę, ilość lub cenę wyposażenia.
5. Domyślnie działa **jeden pasek przy masterze**, a jego etykieta wskazuje cel.
   Kliknięcie lub wejście klawiaturą do detailu przełącza pasek na ten detail.
   Kliknięcie Zapisz/Odrzuć na pasku zachowuje cel i dotyczy tylko jego bufora.
6. Odznacz „Jeden pasek poleceń dla aktywnej tabeli”, aby udostępnić osobne paski.
   Jeśli detail ma pasek zamknięty, włącz go przez Elementy → Pasek poleceń.
   Przy ponownym włączeniu wspólnego trybu opcja ta staje się niedostępna.
   Przy małej szerokości pasek pokazuje ikony z podpowiedziami.

Każda tabela ma własną sesję i bufor. Zwinięcie detailu nie odrzuca jego zmian.
Przed zamknięciem okna trzeba zakończyć edycję i zapisać albo odrzucić bufory.
Próba zamknięcia z oczekującymi zmianami pozostawia okno otwarte z komunikatem.
To oddzielne transakcje zapisu, **bez jednej transakcji całej rodziny**.

## Skąd dane i gdzie zapis

Wykorzystujemy istniejącą bazę 24 pozycji z demo 05. Przy pierwszym uruchomieniu
kopiujemy ją do `%LOCALAPPDATA%/PhialeTech/GridSamples/08-NestedGrid/locations.sqlite`.
Dodajemy tabelę `locations` na kontakt, telefon i uwagi sześciu lokalizacji.
Zmiany pozostają po restarcie; baza wzorcowa w repozytorium nie jest modyfikowana.
To przykład jednego operatora, bez protokołu konfliktów master/detail i bez
przenoszenia wyposażenia między lokalizacjami. Edytujemy istniejące rekordy;
dodawanie i usuwanie nie są scenariuszem tej lekcji ani portów zapisu.

| Plik | Za co odpowiada |
| --- | --- |
| Models/LocationRow.cs | Pola mastera; jego klucz pozostaje stały |
| ViewModels/LocationsViewModel.cs | Kontekst MVVM oraz czytelny builder kolumn mastera |
| Infrastructure/LocationRepository.cs | Robocza baza, tabela lokalizacji i odczyt |
| Infrastructure/LocationCommitPort.cs | Zapis tylko bufora mastera w transakcji |
| Composition/LocationGridComposition.cs | Builder mastera oraz osobna sesja wyposażenia na klucz rodzica |
| Configuration/AssetRelation.cs | Klucze relacji, Source, edytowalne kolumny detailu i jego standardowy pasek |
| Views/MainWindow.xaml.cs | Natywne BindDetailGrid i ochrona przed utratą niezapisanych zmian |

## Dwie ścieżki zapisu

```text
master → jego bufor → LocationCommitPort → tabela locations
detail → jego bufor → SqliteCommitPort   → tabela assets
```

`CommitUsing` umożliwia zapis. Samo `Editable` tylko dopuszcza edycję pola.
`Clone(row => row with { })` tworzy roboczą kopię rekordu; modele zawierają tutaj
wartości proste. To nie jest ogólne kopiowanie głębokie dowolnego obiektu.
`Source` relacji wywołuje `OpenAssets(parent, token)`. Sesja wybiera wyłącznie
wyposażenie tego rodzica. Sześć niewielkich zbiorów mieści się w pamięci.
Kompozycja przechowuje sesje do zamknięcia okna, także gdy detail jest zwinięty.

Do projektu dołączamy pliki AssetRow, AssetColumns, AssetBusinessRules,
AssetRepository i SqliteCommitPort z demo 05. Dzięki temu walidacja i zapis
wyposażenia są już znane z poprzedniej lekcji; projekty nie zależą od swoich EXE.
Filtry i zaznaczenie każdego detailu są niezależne. Układ kolumn relacji jest
domyślnie wspólny. Aplikacja nie zwalnia pożyczonego API dziecka.

## Sprawdź samodzielnie

- Zmień kontakt i nazwę wyposażenia. Zapisz tylko master: wyposażenie nadal ma bufor.
- Zwiń i otwórz detail; jego niezapisana zmiana pozostaje dostępna.
- Odrzuć bufor detailu albo zapisz go i otwórz aplikację ponownie.
- Spróbuj zapisać nieprawidłową ilość; port zapisu odrzuca dane, nie kasując bufora.

Kod i dane syntetyczne: MIT; zależności zachowują własne licencje.
Wersje paczek wynikają ze wspólnej konfiguracji repozytorium i opcjonalnego
LocalPackages.props. Domyślny zestaw tego wydania: Grid preview.12 i Design preview.49.

## Opcja wspólnego paska

```xml
<!-- Tylko na kontrolce MASTER. False pozostawia dotychczasowe osobne paski. -->
<grid:PhialeGrid UseSharedDetailCommandBar="True" ... />
```

Ta sama opcja istnieje na WPF i Avalonii. Core śledzi aktywnego członka rodziny;
frontend podłącza jego istniejące polecenia do standardowego paska mastera.
Nie powstaje wspólny bufor ani zapis całej rodziny. Liczniki, dostępność akcji
i ukrywanie Zapisz/Odrzuć w Immediate pochodzą z aktywnej tabeli. Wspólny tryb
blokuje region własnego paska detailu także w menu. Zwinięcie aktywnego detailu
przenosi cel na master, bez odrzucania zmian. Własne przyciski aplikacji zachowują
swoje powiązania; opcja dotyczy standardowego paska Grida.
