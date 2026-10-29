# .NET Mesajlaşma Uygulaması

PostgreSQL üzerinde sohbet ve mesajları saklayan bir ASP.NET Core REST API ve API'ye bağlanan .NET terminal istemcisi. İstemci yeni mesajları belirli aralıklarla sorgular (**polling**); gerçek zamanlı bağlantı için SignalR veya WebSocket kullanılmaz.

## Özellikler

- Sohbet oluşturma, listeleme ve ayrıntılarını görüntüleme
- Sohbet geçmişini görüntüleme ve mesaj gönderme
- Yeni mesajları artan mesaj ID'sine göre polling ile alma
- İstek doğrulama ve `ProblemDetails` biçiminde merkezi API hata yanıtları
- Entity Framework Core ve PostgreSQL ile kalıcı veri saklama
- İstemci tarafında kullanıcı adı, API adresi ve polling aralığı seçenekleri

## Teknolojiler

- .NET 8 / ASP.NET Core Web API
- .NET Console
- Entity Framework Core 8
- PostgreSQL 14+

## Proje yapısı

```text
StajMesajlasma.sln
├── ChatApi/
│   ├── Controllers/   # HTTP endpoint'leri
│   ├── Data/          # DbContext ve EF Core migration'ları
│   ├── Dtos/          # API istek ve yanıt modelleri
│   ├── Entities/      # Conversation ve Message veri modelleri
│   ├── Middleware/    # Merkezi hata yanıtı
│   └── Services/      # Uygulama kuralları ve işlemleri
└── ChatClient/
    ├── Api/           # HTTP API istemcisi
    ├── Configuration/ # Komut satırı seçenekleri
    ├── Dtos/          # İstemcinin kullandığı API modelleri
    └── Ui/            # Menü, terminal ve polling akışı
```

## Gereksinimler

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL 14 veya üzeri
- Migration komutları için `dotnet-ef` 8 aracı

## Kurulum ve çalıştırma

Aşağıdaki komutları çözüm dosyasının bulunduğu proje kök dizininde çalıştırın.

### 1. Veritabanını oluşturun

PostgreSQL'de `staj_mesaj` adında bir veritabanı oluşturun:

```sql
CREATE DATABASE staj_mesaj;
```

Uygulamanın varsayılan bağlantısı `localhost:5432` üzerindeki `postgres` kullanıcısını kullanır. Yerel PostgreSQL kurulumunuz parola gerektiriyorsa bağlantı bilgisini ortam değişkeniyle sağlayın; gerçek parolaları kaynak dosyalarına veya Git deposuna eklemeyin.

Linux/macOS'ta `.env.example` dosyasını kopyalayıp yerel bağlantı dizgenize göre düzenleyin:

```bash
cp .env.example .env
```

`.env`, Git tarafından yok sayılır. .NET `.env` dosyalarını kendiliğinden yüklemediğinden, migration ve API komutlarından önce değerleri geçerli terminal oturumuna aktarın:

```bash
set -a
. ./.env
set +a
```

PowerShell kullanıyorsanız bağlantı bilgisini terminal oturumunda tanımlayın:

```powershell
$env:ConnectionStrings__ChatDatabase = "Host=localhost;Port=5432;Database=staj_mesaj;Username=postgres"
```

Parola gerektiren bir PostgreSQL kurulumunda, kendi bağlantı dizgenize parolanızı yerel olarak ekleyin. Bu değeri README'ye veya başka bir kaynak dosyasına koymayın.

### 2. EF Core aracını kurun ve migration'ları uygulayın

`dotnet-ef` kurulu değilse:

```bash
dotnet tool install --global dotnet-ef --version 8.0.11
```

Veritabanı tablolarını migration'lardan oluşturun:

```bash
dotnet ef database update --project ChatApi
```

### 3. API'yi çalıştırın

```bash
dotnet run --project ChatApi
```

API varsayılan olarak `http://localhost:5000` adresinde çalışır. Geliştirme ortamında Swagger arayüzü `http://localhost:5000/swagger` adresindedir.

### 4. Terminal istemcisini çalıştırın

Varsayılan olarak istemci `http://localhost:5000` adresine bağlanır ve iki saniyede bir yeni mesajları sorgular:

```bash
dotnet run --project ChatClient
```

API adresi ve polling süresi özelleştirilebilir:

```bash
dotnet run --project ChatClient -- --api-url http://localhost:5000 --poll-seconds 2
```

`--api-url=...` ve `--poll-seconds=...` biçimleri de desteklenir. Polling aralığı 1–60 saniye olmalıdır. İstemci açılışta kullanıcı adını ister. Menüden sohbet oluşturabilir, sohbetleri listeleyebilir veya mevcut bir sohbete katılabilirsiniz. Sohbet ekranında mesajı Enter ile gönderin; `/back` yazarak menüye dönün.

## API uç noktaları

| Metot | Adres | Açıklama |
|---|---|---|
| `POST` | `/api/conversations` | Sohbet oluşturur |
| `GET` | `/api/conversations` | Sohbetleri listeler |
| `GET` | `/api/conversations/{id}` | Sohbet ayrıntısını, mesaj sayısını ve katılımcıları getirir |
| `GET` | `/api/conversations/{id}/messages` | Sohbet mesaj geçmişini getirir |
| `GET` | `/api/conversations/{id}/messages?after={messageId}` | Belirtilen ID'den büyük ID'li mesajları getirir |
| `POST` | `/api/conversations/{id}/messages` | Sohbete mesaj gönderir |

Örnek sohbet oluşturma isteği:

```http
POST /api/conversations
Content-Type: application/json

{
  "title": "Staj sohbeti"
}
```

Örnek mesaj gönderme isteği:

```http
POST /api/conversations/1/messages
Content-Type: application/json

{
  "sender": "Ali",
  "content": "Merhaba!"
}
```

Başarılı oluşturma istekleri `201 Created`, geçersiz istekler `400 Bad Request`, bulunmayan kaynaklar `404 Not Found` döndürür. Beklenmeyen sunucu hataları `500` durum kodu ve `ProblemDetails` biçiminde yanıtlanır.

## Veri modeli ve doğrulama

- **Conversation:** `Id`, `Title` (en fazla 100 karakter), `CreatedAt`
- **Message:** `Id`, `ConversationId`, `Sender` (en fazla 50 karakter), `Content` (en fazla 2000 karakter), `SentAt`

Bir sohbetin birden fazla mesajı olabilir. Sohbet silinirse ilişkili mesajları da silinir. Tarihler UTC olarak saklanır. Boş başlık, gönderen adı veya mesaj içeriği kabul edilmez.

## Yerel ağdan bağlanma

1. API bilgisayarının yerel IP adresini öğrenin (`ipconfig` veya `hostname -I`).
2. API bilgisayarındaki güvenlik duvarında TCP `5000` portuna erişime izin verin.
3. API'yi tüm ağ arayüzlerinde başlatın:

   ```bash
   dotnet run --project ChatApi --urls http://0.0.0.0:5000
   ```

4. İstemcileri API bilgisayarının IP adresini kullanarak başlatın:

   ```bash
   dotnet run --project ChatClient -- --api-url http://<API_IP>:5000
   ```

İki istemci aynı sohbet numarasına girerek mesajlaşabilir. Uygulamada kullanıcı doğrulaması yoktur; API'yi güvenilmeyen ağlara açmayın ve yerel ağ erişimini güvenlik duvarıyla sınırlandırın.
