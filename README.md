# Staj Assignment 1 — Basit Mesajlaşma Uygulaması

PostgreSQL üzerinde kalıcı veri tutan bir mesajlaşma API'si ve bu API'ye bağlanan terminal istemcisi. İstemci yeni mesajları belirli aralıklarla sorgular (polling); SignalR veya WebSocket kullanılmaz.

## Ödevin amacı

ASP.NET Core Web API ve .NET console uygulaması geliştirerek aşağıdaki konularda pratik kazanmak:

- REST ilkelerine uygun API tasarımı ve DTO kullanımı
- Entity Framework Core ile PostgreSQL'e erişim ve migration yönetimi
- Katmanlı kod organizasyonu, validasyon ve hata yönetimi
- HTTP üzerinden çalışan terminal istemcisi ve polling

## Gereksinimler

- .NET 8 SDK
- PostgreSQL 14 veya üzeri
- API bilgisayarında ve her istemci bilgisayarında ağa erişim

## Proje yapısı

```text
StajMesajlasma.sln
├── ChatApi/
│   ├── Controllers/   # HTTP endpoint'leri
│   ├── Data/          # EF Core DbContext ve migration'lar
│   ├── Dtos/          # API istek ve yanıt modelleri
│   ├── Entities/      # Veritabanı varlıkları
│   ├── Middleware/    # Merkezi API hata yanıtları
│   └── Services/      # Uygulama kuralları ve veri erişim akışı
└── ChatClient/
    ├── Api/           # HTTP API istemcisi
    ├── Configuration/ # Komut satırı seçenekleri
    ├── Models/        # API yanıt ve istek modelleri
    └── Ui/            # Menü, terminal ve polling akışı
```

## İşlevler

- Sohbet oluşturma ve sohbetleri listeleme
- Sohbet ayrıntısında mesaj sayısını ve katılımcıları gösterme
- Sohbet geçmişini görüntüleme ve yeni mesaj gönderme
- Polling ile yeni mesajları alma
- Boş/geçersiz girdileri doğrulama; bulunmayan sohbetler için anlamlı hata verme
- Entity'leri dışarı açmak yerine API DTO'ları kullanma

### Veri modeli

- **Conversation:** `Id`, `Title` (en fazla 100 karakter), `CreatedAt`
- **Message:** `Id`, `ConversationId`, `Sender` (en fazla 50 karakter), `Content` (en fazla 2000 karakter), `SentAt`

Bir sohbetin birden fazla mesajı vardır. Sohbet silinirse ilişkili mesajları da silinir. Tarihler UTC olarak saklanır.

### API uç noktaları

| Method | Route | Açıklama |
|---|---|---|
| `POST` | `/api/conversations` | Sohbet oluşturur |
| `GET` | `/api/conversations` | Sohbetleri listeler |
| `GET` | `/api/conversations/{id}` | Sohbet ayrıntısını, mesaj sayısını ve katılımcıları getirir |
| `GET` | `/api/conversations/{id}/messages` | Mesaj geçmişini getirir |
| `GET` | `/api/conversations/{id}/messages?after={id}` | Verilen mesaj ID'sinden sonraki mesajları getirir (polling) |
| `POST` | `/api/conversations/{id}/messages` | Sohbete mesaj gönderir |

Başarılı oluşturma istekleri `201`, geçersiz istekler `400`, bulunmayan sohbetler `404` döndürür. Beklenmeyen API hataları `ProblemDetails` biçiminde yanıtlanır.

## Kurulum ve çalıştırma

Komutları çözümün kök dizininde (`StajMesajlasma.sln` dosyasının bulunduğu dizin) çalıştırın.

### 1. PostgreSQL veritabanını hazırlayın

PostgreSQL'de `staj_mesaj` veritabanı oluşturun:

```sql
CREATE DATABASE staj_mesaj;
```

Varsayılan bağlantı `localhost:5432` üzerinde `postgres` kullanıcısını kullanır ve parola içermez. PostgreSQL kurulumunuz parola gerektiriyorsa bağlantı bilgisini ortam değişkeniyle sağlayın. Gerçek parolaları `appsettings.json` dosyasına veya depoya eklemeyin.

Linux/macOS:

```bash
export ConnectionStrings__ChatDatabase='Host=localhost;Port=5432;Database=staj_mesaj;Username=postgres;Password=parolaniz'
```

PowerShell:

```powershell
$env:ConnectionStrings__ChatDatabase = "Host=localhost;Port=5432;Database=staj_mesaj;Username=postgres;Password=parolaniz"
```

### 2. Veritabanı şemasını oluşturun

EF Core CLI aracı yüklü değilse bir kez kurun:

```bash
dotnet tool install --global dotnet-ef --version 8.0.11
```

Migration'ları uygulayın:

```bash
dotnet ef database update --project ChatApi
```

### 3. API'yi başlatın

```bash
dotnet run --project ChatApi
```

API `http://localhost:5000` adresinde çalışır. Geliştirme ortamında Swagger arayüzü `http://localhost:5000/swagger` adresindedir. API'yi yerel ağdaki diğer bilgisayarların erişimine açmak için ayrıca [Yerel ağdan bağlanma](#yerel-ağdan-bağlanma) bölümünü uygulayın.

### 4. Console istemcisini başlatın

Varsayılan adres ve 2 saniyelik polling aralığı:

```bash
dotnet run --project ChatClient
```

API adresi ve polling aralığı seçenekleri:

```bash
dotnet run --project ChatClient -- --api-url http://localhost:5000 --poll-seconds 2
```

`--api-url=...` ve `--poll-seconds=...` biçimleri de desteklenir. Polling aralığı 1–60 saniye arasında olmalıdır. İstemci açılışta kullanıcı adını ister; menüden sohbet oluşturabilir, sohbetleri listeleyebilir veya bir sohbete girebilirsiniz. Sohbet ekranında mesajı Enter ile gönderin, `/back` yazarak menüye dönün.

## Yerel ağdan bağlanma

1. API bilgisayarının yerel IP adresini öğrenin. Linux/macOS'ta `hostname -I`, Windows'ta `ipconfig` kullanılabilir.
2. API bilgisayarında TCP `5000` portuna gelen bağlantılara güvenlik duvarında izin verin.
3. API'yi tüm ağ arayüzlerinde dinleyecek şekilde başlatın:

   ```bash
   dotnet run --project ChatApi --urls http://0.0.0.0:5000
   ```

4. İstemcileri API bilgisayarının IP adresiyle başlatın:

   ```bash
   dotnet run --project ChatClient -- --api-url http://<API_IP>:5000
   ```

İki istemci aynı sohbet numarasına girerek mesajlaşabilir. PostgreSQL başka bir bilgisayarda bulunuyorsa, API sunucusundan PostgreSQL'e ağ erişimi ve PostgreSQL tarafında uygun `listen_addresses`/`pg_hba.conf` ayarları da gerekir.

> Uygulamada kullanıcı doğrulaması yoktur. Güvenilmeyen ağlara açmayın; yerel ağ erişimini güvenlik duvarıyla sınırlandırın.

## Kabul kontrol listesi

- [ ] PostgreSQL bağlantısı ve migration sorunsuz çalışıyor.
- [ ] API sohbet oluşturabiliyor, listeleyebiliyor ve ayrıntılarını döndürebiliyor.
- [ ] Boş başlık/mesaj ve uzunluğu aşan girdiler reddediliyor.
- [ ] Sohbet geçmişi açılışta gösteriliyor ve polling yeni mesajları getiriyor.
- [ ] İki ayrı istemci aynı sohbette mesajlaşabiliyor.
- [ ] Geçersiz sohbet numarası veya API bağlantı hatası istemciye anlaşılır biçimde bildiriliyor.
