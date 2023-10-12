# Aplikacja SLAVE

## Przegląd

Aplikacja SLAVE, opracowana w C# przy użyciu WPF, jest zaprojektowana do współpracy z aplikacją MASTER w celu realizacji jednokierunkowej synchronizacji plików. Zmiany (tworzenie i usuwanie plików) w katalogu monitorowanym przez MASTER są transmitowane i odtwarzane w aplikacji SLAVE.

## Funkcje

- **Przycisk START**: Rozpoczyna synchronizację katalogu.
- **Przycisk STOP**: Zatrzymuje synchronizację katalogu.
- **Przycisk Wyboru Katalogu Zapisu**: Wybiera katalog, w którym będą pojawiać się pliki przesłane z katalogu MASTERA. Ścieżka do wybranego katalogu jest zapisywana i aktualizowana w pliku konfiguracyjnym XML.
- **Pole tekstowe Adresu IP Mastera**: Określa adres IP komputera z aplikacją MASTERA.
- **Pole tekstowe Portu Komunikacji UDP z MASTEREM**: Określa port, przez który aplikacja będzie wysyłać dane do MASTERA.

## Konfiguracja

Aplikacja SLAVE używa pliku konfiguracyjnego XML do odczytu danych konfiguracyjnych podczas uruchomienia, zapewniając trwałość ustawień między użyciami.

## Protokół

Aplikacja SLAVE komunikuje się z MASTER za pomocą własnoręcznie zaprojektowanego protokołu przez UDP. Ten protokół zapewnia mechanizmy inicjowania komunikacji, przesyłania informacji o utworzonych plikach (dane binarne do skopiowania) oraz typ zdarzenia (utworzenie lub usunięcie pliku).

## Testowanie

Ta aplikacja jest przeznaczona do testowania na jednym komputerze. Adres IP jest ustawiony w aplikacji jako `127.0.0.1`.

