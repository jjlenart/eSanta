# Projekt

Aplikacja jest tworzona w .NET 10 i Blazor Server.

# Architektura

- Stosuj czystą architekturę.
- Logika biznesowa nie może znajdować się w komponentach Razor.
- Komponenty Razor mają zawierać wyłącznie logikę UI.
- Dostęp do danych realizuj przez serwisy.
- Korzystaj z Dependency Injection.
- Unikaj statycznych klas.

# Baza danych

- PostgreSQL.
- Entity Framework Core.
- Wszystkie zmiany schematu wykonuj przez migracje EF Core.
- Podejście DB first
- Nie używaj surowego SQL, chyba że zostanie o to poproszony.
- Twórz poprawne klucze obce i indeksy.

# Kod

- Nazwy klas, metod i właściwości po angielsku.
- Komentarze tylko jeśli są naprawdę potrzebne.
- Preferuj czytelność zamiast nadmiernej abstrakcji.
- Twórz małe klasy z jedną odpowiedzialnością.
- Stosuj async/await dla operacji I/O.

# UI

- Używaj MudBlazor.
- Projektuj responsywny interfejs.
- Zachowuj spójny wygląd wszystkich formularzy.
- Pokazuj komunikaty błędów użytkownikowi.

# Bezpieczeństwo

- Walidacja danych po stronie klienta i serwera.
- Nie przechowuj sekretów w kodzie.
- Connection stringi trzymaj w konfiguracji.

# Testy

- Twórz testy jednostkowe dla logiki biznesowej.
- Używaj xUnit.
- Używaj biblioteki Shouldly

# Wymagania dla zmian

Przed wprowadzeniem zmian:
1. Opisz plan.
2. Wskaż pliki do modyfikacji.
3. Dopiero potem wykonaj zmiany.