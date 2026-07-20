# Provision Tool - Professional Enterprise Deployment

## 📋 Description

Provision Tool — это современное десктопное приложение на C# + WPF для параллельного SSH развёртывания и конфигурирования множества устройств (price checkers, IoT devices и т.д.). Приложение обеспечивает полностью автоматизированный процесс прошивки, проверки доступности и генерации отчётов.

## ✨ Key Features

✅ **Параллельное развёртывание** — подключение и прошивка до 50 устройств одновременно (настраивается)
✅ **Автоматическое заполнение** — быстрое заполнение таблиц IP-адресов и хостнеймов
✅ **Мониторинг перезагрузки** — отслеживание перезагрузки устройств через PING
✅ **Проверка конфигурации** — верификация SSH-подключения и хостнейма после развёртывания
✅ **Логирование** — детальные логи всех операций в реальном времени
✅ **Сохранение сессии** — автоматическое сохранение данных и настроек
✅ **Резервный SSH** — поддержка основного и резервного логина/пароля
✅ **CSV импорт/экспорт** — загрузка и выгрузка списков устройств
✅ **Тёмный дизайн** — современный интерфейс, вдохновлённый Visual Studio 2022 и JetBrains Rider
✅ **Асинхронные операции** — неблокирующее выполнение с поддержкой отмены

## 🏗️ Architecture

```
ProvisionTool/
├── Models/                 # Модели данных
│   ├── DeploymentDevice.cs
│   ├── DeploymentSettings.cs
│   └── DeploymentResult.cs
├── Services/              # Бизнес-логика
│   ├── ISshService.cs
│   ├── SshService.cs
│   ├── IDeploymentService.cs
│   ├── DeploymentService.cs
│   ├── IStorageService.cs
│   ├── StorageService.cs
│   ├── ICsvService.cs
│   ├── CsvService.cs
│   └── ServiceFactory.cs
├── ViewModels/            # MVVM ViewModels
│   ├── MainViewModel.cs
│   ├── LogViewerViewModel.cs
│   └── SettingsViewModel.cs
├── Views/                 # WPF окна и контролы
│   ├── MainWindow.xaml
│   ├── DeviceLogViewer.xaml
│   └── SettingsWindow.xaml
├── Utils/                 # Утилиты и конвертеры
│   ├── Logger.cs
│   ├── ValidationHelper.cs
│   ├── Converters.cs
│   ├── CollectionExtensions.cs
│   └── StringExtensions.cs
└── Resources/             # Ресурсы
    └── Styles.xaml
```

## 🚀 Usage

### Installation

```bash
# Clone repository
git clone https://github.com/NichoIass/NichoIass.git
cd NichoIass

# Open in Visual Studio 2019+
start ProvisionTool.sln

# Build
Ctrl+Shift+B

# Run
F5
```

### Quick Start

1. **Введите учётные данные SSH**
   - Primary SSH: основной логин/пароль
   - Backup SSH: резервный логин/пароль
   - Скрипт прошивки URL

2. **Заполните таблицу**
   - Введите SSH IP (тимчасовий адрес)
   - Введите Target IP (окончательный адрес)
   - Введите Hostname (желаемое имя)

3. **Используйте автозаполнение**
   - Нажмите "Autofill: SSH IP" для автоматического заполнения
   - Нажмите "Autofill: Target IP" для IP адресов
   - Нажмите "Autofill: Hostname" для хостнеймов

4. **Запустите развёртывание**
   - "Start All" — для всех заполненных устройств
   - "Start Selected" — только для отмеченных

5. **Мониторьте процесс**
   - Смотрите статусы в реальном времени
   - Просмотрите логи каждого устройства
   - Проверьте отчёт по завершении

## 🔧 Requirements

- Windows 7 SP1+ или Windows Server 2012+
- .NET 6.0 Runtime (или .NET Framework 4.7.2+)
- Visual Studio 2019+ (для разработки)

## 📦 Dependencies

- **SSH.NET** — SSH клиент для подключения
- **CommunityToolkit.Mvvm** — MVVM framework
- **WPF** — графический фреймворк

## 🎨 UI Design

### Color Scheme
- Background: #0C0D0F
- Surface: #151619
- Accent: #6C8CFF
- Success: #48D597
- Danger: #F1667C
- Info: #57C4E8

### Typography
- Body: Segoe UI 12-14px
- Mono: Consolas 12-13px
- Headers: 24-28px SemiBold

## 🔒 Security

- ✅ Пароли хранятся в памяти приложения
- ✅ SSH подключение через SFTP/SCP
- ✅ Поддержка SSH ключей (future)
- ✅ Логирование всех операций

## 📊 Performance

- ⚡ Асинхронные операции I/O
- ⚡ Параллельные подключения (до 50)
- ⚡ Эффективное управление памятью
- ⚡ Оптимизированная UI

## 📝 License

MIT

## 👨‍💻 Author

NichoIass — Enterprise Software Developer

## 🐛 Known Issues

None at the moment. Please report any issues via GitHub Issues.

## 🚧 Roadmap

- [ ] SSH Key Management UI
- [ ] Multi-language support
- [ ] Dark mode toggle
- [ ] Advanced filtering and sorting
- [ ] Device templates
- [ ] Scheduled deployments
- [ ] REST API

## 📧 Contact

For support and inquiries: [GitHub Issues](https://github.com/NichoIass/NichoIass/issues)
