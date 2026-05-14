# Практическая работа 3 Шаблоны проектов

Внутри архива два шаблона, один на C#, второй на Node.js.

Оба шаблона показывают работу с настройками из трёх источников, проверку корректности настроек до запуска, базовую защиту для запросов из браузера, ограничение частоты запросов и добавление защитных заголовков.

## Приоритет источников настроек

Порядок переопределения одинаковый в обоих шаблонах:

1. Файл настроек
2. Переменные окружения
3. Аргументы командной строки

## Критичные настройки

- `trustedOrigins` / `App:TrustedOrigins` - без списка доверенных источников служба не запускается.
- `rateLimits` / `App:RateLimits` - лимиты чтения и записи должны быть больше нуля, запись не должна быть выше чтения.
- `mode` / `App:Mode` - только учебный или боевой режим.

Ранняя проверка выполняется до запуска веб службы. При ошибках выводится понятная причина и запуск прерывается.

## Запуск C# варианта

### Учебный режим

```powershell
cd d:\task3_framework\csharp\src\Pr3.ConfigAndSecurity
dotnet run -- --mode Учебный --origins "http://localhost:5173" --readPerMinute 60 --writePerMinute 20
```

### Боевой режим

```powershell
cd d:\task3_framework\csharp\src\Pr3.ConfigAndSecurity
dotnet run -- --mode Боевой --origins "http://localhost:5173" --readPerMinute 30 --writePerMinute 10
```

### Переменные окружения

```powershell
$env:PR3_App__Mode = "Учебный"
$env:PR3_App__TrustedOrigins__0 = "http://localhost:5173"
$env:PR3_App__RateLimits__ReadPerMinute = "60"
$env:PR3_App__RateLimits__WritePerMinute = "20"
dotnet run
```

### Проверки C#

```powershell
cd d:\task3_framework\csharp
dotnet test
```

## Запуск Node.js варианта

### Учебный режим

```powershell
cd d:\task3_framework\node
node src\server.js --mode=учебный --port=3000 --trustedOrigins=http://localhost:5173 --readPerMinute=60 --writePerMinute=20
```

### Боевой режим

```powershell
cd d:\task3_framework\node
node src\server.js --mode=боевой --port=3000 --trustedOrigins=http://localhost:5173 --readPerMinute=30 --writePerMinute=10
```

### Переменные окружения

```powershell
cd d:\task3_framework\node
$env:APP_MODE = "учебный"
$env:APP_PORT = "3000"
$env:APP_TRUSTED_ORIGINS = "http://localhost:5173"
$env:APP_READ_PER_MINUTE = "60"
$env:APP_WRITE_PER_MINUTE = "20"
node src\server.js
```

### Проверки Node.js

```powershell
cd d:\task3_framework\node
npm test
```

## Что проверяют тесты

- Приоритет источников настроек (файл -> окружение -> аргументы).
- Остановка запуска при некорректных настройках.
- CORS только для доверенных источников.
- Ответ 429 при превышении лимита.
- Наличие защитных заголовков.