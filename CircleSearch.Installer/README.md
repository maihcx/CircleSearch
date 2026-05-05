# CircleSearch Installer

Trình cài đặt WPF cho ứng dụng **CircleSearch**, hỗ trợ đa ngôn ngữ (Tiếng Việt / English) và tích hợp gỡ cài đặt qua Control Panel / Windows Settings.

---

## Cấu trúc project

```
CircleSearch.Installer/
├── App.xaml / App.xaml.cs         - Application entry point, phát hiện --uninstall flag
├── app.manifest                   - Yêu cầu quyền Administrator
├── build.bat                      - Script build & đóng gói
│
├── Assets/
│   ├── app.ico                    - Icon ứng dụng
│   └── app-256.png                - Logo hiển thị trong sidebar
│
├── Resources/
│   └── Locales/
│       ├── String.resx            - Neutral (fallback keys)
│       ├── String.en.resx         - English
│       ├── String.vi.resx         - Tiếng Việt
│       └── String.Designer.cs     - Auto-generated accessor
│
├── Utils/
│   ├── Translation.cs             - TranslationSource, LocalizationExtension, LanguageBase
│   └── StepConverters.cs          - IValueConverter cho sidebar step indicator
│
├── ViewModels/
│   └── InstallerViewModel.cs      - Toàn bộ logic điều hướng các bước
│
├── Views/
│   ├── MainWindow.xaml            - UI đa bước (step-by-step)
│   └── MainWindow.xaml.cs         - Code-behind
│
└── Services/
    └── InstallService.cs          - Logic cài đặt / gỡ cài đặt thực tế
```

---

## Yêu cầu

- **.NET 8 SDK** trở lên  
- **Windows 10 / 11** (x64)
- Chạy với quyền **Administrator** (do `app.manifest` yêu cầu)

---

## Build

### 1. Chuẩn bị

Đặt project này ngang với thư mục gốc của CircleSearch:

```
CircleSearch/           ← project gốc
  publish/              ← output sau khi chạy build.bat gốc
    CircleSearch.exe
    CircleSearch.Tray.exe
    CircleSearch.Core.exe
    CircleSearch.Overlay.exe
  build.bat             ← build gốc
CircleSearch.Installer/ ← project này
  build.bat             ← build installer
```

### 2. Build CircleSearch trước

```bat
cd CircleSearch
build.bat
```

### 3. Build Installer

```bat
cd CircleSearch.Installer
build.bat
```

Output sẽ nằm ở `installer-output\CircleSearch.Installer.exe` — **một file exe duy nhất**, self-contained.

---

## Cách hoạt động

### Cài đặt

1. User chạy `CircleSearch.Installer.exe`
2. **Chọn ngôn ngữ** (VI / EN) → giao diện chuyển ngôn ngữ ngay lập tức
3. **Màn hình chào mừng**
4. **License Agreement** – phải tick Accept
5. **Chọn thư mục cài đặt** (mặc định: `%ProgramFiles%\CircleSearch`)
6. **Tùy chọn** – shortcut desktop, Start Menu, khởi động cùng Windows
7. **Cài đặt** – copy files, tạo shortcut, ghi registry uninstaller
8. **Hoàn tất** – tùy chọn khởi động ứng dụng ngay

### Gỡ cài đặt

Có 2 cách:

**Cách 1:** Qua **Control Panel → Programs → Uninstall a program** → chọn CircleSearch → Uninstall  
**Cách 2:** Qua **Windows Settings → Apps → Installed Apps** → CircleSearch → Uninstall

Cả hai đều gọi:
```
CircleSearch.Installer.exe --uninstall
```

Trình gỡ cài đặt sẽ:
1. Cho user chọn ngôn ngữ
2. Xác nhận gỡ cài đặt
3. Kill process CircleSearch đang chạy
4. Xóa thư mục cài đặt
5. Xóa shortcuts (Desktop, Start Menu)
6. Xóa registry key startup
7. Xóa registry key uninstaller

---

## Đa ngôn ngữ (i18n)

Sử dụng cùng kiến trúc với `CircleSearch.Tray`:

- **`TranslationSource`** – `INotifyPropertyChanged` singleton, binding-friendly
- **`LocalizationExtension`** – Markup Extension dùng trong XAML: `{local:LocalizationExtension key}`
- Ngôn ngữ được chọn **ngay màn hình đầu tiên** trước khi bắt đầu cài đặt/gỡ cài đặt
- Thêm ngôn ngữ mới: thêm `String.xx.resx` và thêm vào `LanguageBase.SupportedLanguages`

---

## Registry

Installer ghi vào:
```
HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\CircleSearch
```

Với các giá trị:
| Key | Value |
|-----|-------|
| DisplayName | CircleSearch |
| DisplayVersion | 1.0.0 |
| Publisher | CircleSearch |
| InstallLocation | `<thư mục cài đặt>` |
| DisplayIcon | `<path>\CircleSearch.exe` |
| UninstallString | `"<path>\CircleSearch.Installer.exe" --uninstall` |
| QuietUninstallString | `"<path>\CircleSearch.Installer.exe" --uninstall --quiet` |
| NoModify | 1 |
| NoRepair | 1 |
| EstimatedSize | 51200 (KB) |
