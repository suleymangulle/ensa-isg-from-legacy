# `info@saglamosgb.com.tr` — firmaların yeni sistemde görünmeme nedeni

**Tarih:** 2026-08-29
**Kapsam:** salt-okunur inceleme. İncelemenin kendisinde tek satır uygulama kodu yazılmadı, hiçbir
kaynak dosya değiştirilmedi; yalnızca bu rapor oluşturuldu ve doğrulama sırasında bu oturumun kendi
ürettiği test kayıtları silindi (bkz. [§1](#1-silinen-sahte-veriler)). İnceleme bittikten sonra
§8'deki seçenekler uygulandı — ne yapıldığı ve sonucun ne olduğu [§11](#11-uygulanan-çözüm)'de.

**İncelenen veritabanları** (`src/Ensa.HttpApi.Host/appsettings.Development.local.json`,
gitignore'lu; kimlik bilgileri bu rapora kopyalanmadı):

| Rol | Sunucu | Veritabanı |
|---|---|---|
| Eski (legacy) | `213.159.30.211,1433` | `DemoOsgbDb` |
| Yeni | `213.159.30.211,1433` | `EnsaDbDEv` |

---

## Kısa cevap

**Firmalar yeni veritabanında eksik değil — 1.791'i de duruyor.** Kullanıcı onları göremiyor,
çünkü hesabı yanlışlıkla **tek bir işyerine kilitlenmiş** durumda.

`UserProfile.CompanyId = 14778` yazılı. Bu değer, ADR-034'ün *company scope* global sorgu
filtresinin anahtarı: doluysa "bu kullanıcı bir müşteri firmasının yetkilisidir" demek ve o kullanıcı
yalnızca o tek işyerini görebilir. Oysa 14778, müşteri işyeri değil — **SAGLAM OSGB'nin kendi firma
kaydı**. Sonuç: 1.536 işyeri yerine **1** kayıt dönüyor.

Bu, veri göçünden gelen bir hata. Sistemde **983 kullanıcıyı** (bunların **713'ü kurum yöneticisi**)
aynı şekilde etkiliyor.

---

## 1. Silinen sahte veriler

Şube değiştirici doğrulaması sırasında bu oturumun `EnsaDbDEv`'de ürettiği kayıtların tamamı
kalıcı olarak (hard delete) silindi:

| Tablo | Adet | Kayıtlar |
|---|---|---|
| `ensa.Office` | 6 | 972, 973, 974, 975, 976, 977 |
| `ensa.Company` | 18 | 46879–46896 |
| `ensa.CompanyEmployee` | 8 | 264147–264154 (yukarıdaki firmalara bağlı) |
| `ensa.Document` | 2 | 114275, 114280 (yukarıdaki firmalara bağlı) |
| `ensa.[User]` | 19 | 8309–8327 (`officecheck*`, `authcheck*`, `uzman*`, `host*`, `yonetici*`, `musteri*`, `kurumadmin*`, `escalation.probe.*`) |
| `ensa.UserProfile` | 19 | yukarıdaki kullanıcıların profilleri |
| `ensa.UserEmployment` | 19 | yukarıdaki kullanıcıların istihdam kayıtları |

**Artık doğrulaması** — hepsi `0`:

```
Office >= 972            0
Company >= 46879         0
User >= 8309             0
Company created >= 17:30 0
Office created >= 17:30  0
orphan UserProfile       0
orphan UserEmployment    0
```

Silme öncesi `Office` ve `Company` tablolarına gelen yabancı anahtar kısıtı olmadığı doğrulandı
(`sys.foreign_keys`: yalnızca Identity'nin `UserClaim`/`UserLogin`/`UserRole`/`UserToken` tabloları
`User`'a bağlı ve hepsi `CASCADE`). Silme işlemleri tablo başına tek tek, açık kimlik listeleriyle
yapıldı.

> **Not:** `27 Ağustos` tarihli `authcheck*` kullanıcıları ve `12:06`/`12:14` saatli
> `Kalici Test Firma …` kayıtları bu oturuma ait değil, önceki çalışmalardan kalmış. Onlara
> dokunulmadı.

---

## 2. Kanıt zinciri

### 2.1 Eski veritabanı — kullanıcı ve firmaları var

`DemoOsgbDb.dbo.Kullanici_T`:

| KullaniciId | Email | KurumId | FirmaId | OfisId | PersonelTuru | Admin | Aktif |
|---|---|---|---|---|---|---|---|
| **1719** | info@saglamosgb.com.tr | 15724 | **15724** | 740 | Admin | 1 | 1 |

`DemoOsgbDb.dbo.Firma_T` — `KurumId = 15724`:

```
Toplam 1790 | Aktif 1683 | Silinmiş 255 | Kurum kaydı 0
```

Kurumun kendi kaydı: `FirmaId = 15724`, `FirmaAdi = SAGLAM OSGB`, `Kurum = 1`, `KurumTuru = OSGB`.
Bu, legacy'deki **en büyük** kurum (1790 firma ile ilk sırada).

### 2.2 Yeni veritabanı — veri eksik değil

`migration.IdMap` üzerinden eşleme:

| Legacy | Legacy Id | Modern Id |
|---|---|---|
| `Firma_T:Kurum` | 15724 | **Organization 847** |
| `Firma_T:KurumSirket` | 15724 | **Company 14778** |
| `Kullanici_T` | 1719 | **User 4785** |
| `Ofisler_T` | 740 | **Office 490** |

`ensa.Organization 847` = `SAGLAM OSGB`, `Code = L15724`, `IsActive = 1`.

`ensa.Company` — `TenantId = 847`:

```
Toplam 1791 | Silinmemiş 1536 | Aktif+silinmemiş 1455 | Silinmiş 255
```

**Firmalar yerinde.** (1791 = legacy'nin 1790'ı + kurumun kendi firma kaydı.)

### 2.3 Hesabın durumu

`ensa.[User] 4785`:

| Alan | Değer |
|---|---|
| `UserName` | `info@saglamosgb.com.tr` |
| `TenantId` | **847** ✔ doğru kurum |
| Rol | `OrganizationAdministrator` |
| `UserType.StaffRole` | 7 (Organization Administrator) |
| `UserProfile.IsActive` | 1 |
| **`UserProfile.CompanyId`** | **14778** ← sorun burada |
| `UserOffice` | tek satır → Office 490 (`SAGLAM OSGB ISTANBUL`, aktif, tenant 847) |

`ensa.Company 14778` = **`SAGLAM OSGB`**, `TenantId = 847` — yani kurumun *kendi* firma kaydı,
bir müşteri işyeri değil.

### 2.4 Canlı API ile üretilen kanıt

API ayağa kaldırılıp bu hesapla oturum açıldı (parola rapora yazılmadı):

```
--- token claims ---
  sub                4785
  name               info@saglamosgb.com.tr
  ensa:tenantId      847
  ensa:companyId     14778          <-- kilidin kaynağı

--- /api/account/profile ---
  tenantId=847  companyId=14778  officeId=490  roles=['OrganizationAdministrator']

--- /api/account/permissions ---
  HTTP 200  count=374              <-- yetki sorunu YOK

--- /api/company?MaxResultCount=1 (şube başlığı yok) ---
  HTTP 200  totalCount=1           <-- beklenen 1536

--- /api/company?MaxResultCount=1 (X-Ensa-OfficeId: all) ---
  HTTP 200  totalCount=1           <-- şube bağlamı sonucu değiştirmiyor

--- /api/account/offices ---
  count=0  default=None  allOfficesAllowed=True
```

SQL ile aynı hesap:

```
tenant847_silinmemis        1536   <-- filtre kapalıyken görülmesi gereken
kapsam_filtresiyle_gorunen     1   <-- Company.Id = 14778
```

API'nin döndürdüğü `totalCount = 1`, filtrenin hesapladığı değerle **birebir** aynı. Neden–sonuç
zinciri kapalı.

---

## 3. Kök neden

### 3.1 Filtre doğru çalışıyor — beslendiği veri yanlış

`src/Ensa.EntityFrameworkCore/EnsaDbContext.cs:333-354`, ADR-034 (company scope):

```csharp
if (typeof(ICompanyScoped).IsAssignableFrom(typeof(TEntity)))
    ... || CurrentCompanyId == null
       || EF.Property<int?>(e, CompanyIdPropertyName) == CurrentCompanyId;

if (typeof(ICompanyRecord).IsAssignableFrom(typeof(TEntity)))     // yalnızca Company
    ... || CurrentCompanyId == null
       || EF.Property<int>(e, IdPropertyName) == CurrentCompanyId;
```

`CurrentCompanyId` = `ICurrentUser.CompanyId` = token'daki `ensa:companyId` claim'i
(`AuthorizationController.cs:418-421`). Dolu olduğu anda:

* `Company` (`ICompanyRecord`) → yalnızca `Id == 14778`,
* `ICompanyScoped` olan 35 varlık (çalışan, risk analizi, eğitim, doküman, fatura, …) →
  yalnızca `CompanyId == 14778`.

Filtre **fail-closed** tasarlanmış (ADR-034): `CompanyId` çözülemeyen kullanıcı her şeyi değil,
hiçbir şeyi görür. Yani filtre kusursuz çalışıyor; hatalı olan `CompanyId` değerinin kendisi.

### 3.2 Değerin nereden geldiği

`src/Ensa.DataMigrator/Steps/UserSplitStep.cs:348-363` — `CompanyScopeAsync`:

```sql
UPDATE p
SET CompanyId = u.CompanyId
FROM ensa.UserProfile AS p
JOIN ensa.[User] AS u ON u.Id = p.UserId
WHERE u.CompanyId IS NOT NULL AND p.CompanyId IS NULL;
```

Bu adım, legacy `Kullanici_T.FirmaId` değerini geçici `User.CompanyId` sütunundan
`UserProfile.CompanyId`'ye taşıyor — **hiçbir kullanıcı türü ayrımı yapmadan**. Metodun kendi XML
yorumu "*The company a **customer** user belongs to*" diyor; kod ise `FirmaId` dolu olan **herkese**
uyguluyor.

`TenancyStep.cs:517` bilinçli olarak `CompanyId = null` bırakıyor ("*FirmaId points at a client
company, which the companies step has not created yet. It is resolved there…*") — yani değeri
yazan tek yer bu `UPDATE`.

### 3.3 Legacy'de `FirmaId` neden zararsızdı

`Kullanici_T.FirmaId` legacy'de "müşteri bağı" değil, çok amaçlı bir sütundu. Eski veriden:

| PersonelTuru | Toplam | FirmaId dolu |
|---|---|---|
| Uzman | 1512 | 246 |
| Doktor | 960 | 33 |
| **Admin** | **766** | **731** |
| **Müşteri** | 286 | **244** |
| Diğer Sağlık | 166 | 0 |
| ofis-admin | 8 | 0 |

Ve kritik ölçüm:

```
FirmaId dolu olan Admin sayısı        731
bunlardan FirmaId = KurumId olanlar   728
```

Yani **OSGB yöneticilerinin `FirmaId`'si kendi kurumlarının firma kaydını gösteriyor**, bir müşteri
işyerini değil.

Legacy kod bunu zaten biliyordu — `ENSA_ISG/Controllers/BaseController.cs:216-234`:

```csharp
public static int FirmaId
{
    get
    {
        int firmaId = 0;
        int.TryParse(...QueryString["firma-id"], out firmaId);
        if (firmaId != 0) Session["FirmaId"] = firmaId;
        else if (Kullanici.PersonelTuru == "Personel") firmaId = Kullanici.FirmaId.Value;   // <-- yalnızca burada
        else if (Session["FirmaId"] != null) int.TryParse(...);
        return firmaId;
    }
}
```

`Kullanici.FirmaId` yalnızca `PersonelTuru == "Personel"` dalında okunuyordu; müşteri kapsamı ise
`PersonelTuru == "Müşteri"` üzerinden (`BaseController.Subeler`, `GenelMethodsController.MusteriMi()`)
belirleniyordu. Firma listesi `FirmaId`'ye hiç bakmıyordu
(`FirmaListController.cs:67` yalnızca `KurumId` + `OfisId` filtreliyor).

Göç, bu sütunu **anlamını daraltarak** taşıdı: legacy'de "bağlamsal bir işaret" olan değer, yeni
modelde "kesin bir güvenlik sınırı" hâline geldi.

---

## 4. Etkinin büyüklüğü

`UserProfile.CompanyId` dolu olan kullanıcıların personel türüne göre dağılımı (`EnsaDbDEv`):

| StaffRole | Ad | Kullanıcı |
|---|---|---|
| 7 | Organization Administrator | **713** |
| 5 | Customer | 323 |
| 1 | Occupational Safety Specialist | **237** |
| 2 | Workplace Physician | **33** |

* **983 kullanıcı** müşteri olmadığı hâlde tek bir işyerine kilitlenmiş.
* **323 kullanıcı** (StaffRole 5 = Müşteri) doğru şekilde kapsamlanmış — bunlara dokunulmamalı.
* Karşılaştırma: `CompanyId` boş olan **142** kurum yöneticisi sorunsuz çalışıyor.

Tenant bazında en çok etkilenenler:

| TenantId | Kurum | Etkilenen yönetici | Kurumun firma sayısı |
|---|---|---|---|
| 582 | Yesil Nokta OSGB | 3 | 0 |
| **847** | **SAGLAM OSGB** | **2** | **1536** |
| 1044 | salt is sagligi ve güv. hizm. | 1 | 41 |
| 981 | SAMSUN MERKEZ OSGB | 1 | 985 |
| 1137 | sarikiz osgb | 1 | 55 |

Sorun tek kuruma özgü değil, veri tabanı geneline yayılmış.

---

## 5. İkincil belirtiler (aynı kök nedenden)

### 5.1 Şube listesi boş dönüyor

`/api/account/offices` bu kullanıcı için **0 şube** döndürüyor — oysa `ensa.UserOffice` tablosunda
Office 490'a bağlı, aktif, silinmemiş bir ataması var.

Sebep: `Office` varlığı `ICompanyScoped` uyguluyor
(`src/Ensa.Domain/Tenancy/Office.cs:9` — `public class Office : FullAuditedTenantEntity, IActivatable, ICompanyScoped`)
ve şubenin `CompanyId` değeri `null`. Company scope filtresi, `CompanyId` boş satırları
"provider-level data" sayıp firmaya bağlı kullanıcıdan **gizliyor** (`EnsaDbContext.cs:335-342`
yorumu bunu açıkça yazıyor). Dolayısıyla kullanıcı kendi atandığı şubeyi bile listeleyemiyor.

### 5.2 Profil ile şube listesi çelişiyor

`/api/account/profile` → `officeId = 490`
`/api/account/offices` → `count = 0`

Profil, `UserOffice` tablosunu doğrudan okuduğu için 490'ı görüyor; şube listesi ise `Office`
tablosundan geçtiği için filtreye takılıyor. Aynı hesap için iki uç iki farklı cevap veriyor.

---

## 6. Şube değiştirici bu olayın nedeni **değil**

Bu ihtimal özellikle test edildi, çünkü şube kapsamı bu oturumda devreye girdi:

| Senaryo | `totalCount` |
|---|---|
| `X-Ensa-OfficeId` başlığı **yok** | 1 |
| `X-Ensa-OfficeId: all` | 1 |

Sonuç iki durumda da aynı. Ayrıca bu kullanıcı için `/api/account/offices` boş liste +
`allOfficesAllowed = true` döndürdüğü için istemci `all` kapsamını seçiyor; `all` kapsamı
`CoversWholeTenant` olduğundan **hiç şube yüklemi üretmiyor**. Yani şube filtresi bu hesapta
devrede bile değil.

Şube özelliği kaldırılsa da kullanıcı yine 1 firma görürdü. Hata, şube özelliğinden **önce de
vardı**.

> **Ancak:** §5.1'deki `Office`/`ICompanyScoped` etkileşimi, `CompanyId` düzeltilmeden şube
> değiştiricinin bu 983 kullanıcı için çalışamayacağı anlamına geliyor. İki konu birbirinden
> bağımsız değil; `CompanyId` düzeltilmeden şube seçici de görünmez.

---

## 7. Değerlendirilen ve elenen diğer olasılıklar

| Hipotez | Sonuç | Dayanak |
|---|---|---|
| Firmalar göç etmedi | **Hayır** | `TenantId = 847` altında 1791 firma var |
| Kullanıcı yanlış tenant'ta | **Hayır** | `User.TenantId = 847`, `Organization 847 = SAGLAM OSGB` |
| Yetki eksik | **Hayır** | `/api/account/permissions` → 374 yetki |
| Hesap pasif / kilitli | **Hayır** | `UserProfile.IsActive = 1`, `LockoutEnd = NULL`, token alınabiliyor |
| Firmalar soft-delete | **Hayır** | 1791'in yalnızca 255'i silinmiş; 1536'sı canlı |
| Şube (office) filtresi | **Hayır** | başlıklı/başlıksız sonuç aynı — §6 |
| Kullanıcı hesabı hiç göç etmedi | **Hayır** | `Kullanici_T 1719 → User 4785` eşlemesi var ve giriş yapılabiliyor |
| Mükerrer e-posta yanlış hesaba düşürüyor | **Hayır** | 13 mükerrer kayıttan yalnızca 4785 temiz `UserName` almış; diğerleri `.7610` gibi ekli — giriş 4785'e düşüyor (token `sub = 4785`) |

### 7.1 Mükerrer e-posta durumu (bilgi amaçlı, sorunun nedeni değil)

Legacy'de `info@saglamosgb.com.tr` **13 farklı kullanıcı satırında** kullanılmış (KurumId 15724,
57730, 35417, …). Göç, benzersiz `UserName` üretmek için legacy id'yi sonek yapmış:

```
4785  info@saglamosgb.com.tr         TenantId=847   <-- giriş bu hesaba düşüyor
7829  info@saglamosgb.com.tr.7610    TenantId=1320
7830  info@saglamosgb.com.tr.7611    TenantId=1320
...
7903  info@saglamosgb.com.tr.7684    TenantId=1049
```

Doğru hesaba düşüyor; ancak diğer 12 hesap bu e-postayla **giriş yapamaz** (kullanıcı adları
soneki taşıyor). Legacy'de bu kişiler e-posta + parola ile giriş yapabiliyordu. Ayrı bir konu
olarak not edilmiştir.

---

## 8. Çözüm seçenekleri (inceleme anında: uygulanmamıştı)

> Bu bölüm inceleme bittiği andaki durumu anlatır. Seçilen yol ve uygulanmış hâli
> [§11](#11-uygulanan-çözüm)'de.

### Seçenek A — Veriyi düzelt (önerilen)
Müşteri olmayan kullanıcılarda `UserProfile.CompanyId` alanını boşalt:

* Etki: 983 kullanıcı (713 kurum yöneticisi dâhil) kendi kurumunun tüm işyerlerini görmeye başlar.
* Dokunulmaması gereken: `StaffRole = 5` (Müşteri) olan 323 kullanıcı.
* Risk: düşük. Filtre fail-closed olduğu için bu değişiklik **kapsamı genişletir**, bu da tam
  olarak istenen davranış — ama kimin gerçekten müşteri olduğunun `StaffRole`'a göre doğru
  belirlendiğinin teyit edilmesi gerekir.
* Geri alınabilirlik: değiştirilen satırların eski değeri yedeklenmeden yapılmamalı.

### Seçenek B — Göç adımını düzelt
`UserSplitStep.CompanyScopeAsync` sorgusuna kullanıcı türü koşulu eklenir (yalnızca legacy
`PersonelTuru = 'Müşteri'` karşılığı olan `StaffRole = 5`). Yeni bir göç çalıştırılacaksa bu şart;
mevcut veriyi tek başına düzeltmez, A ile birlikte gerekir.

### Seçenek C — `Office` varlığını company scope dışına al
§5.1'deki şube görünmezliği için `Office`'in `ICompanyScoped` uygulaması gözden geçirilir. Şube,
kuruma ait bir yapıdır; bir müşteri işyerine ait değildir. A uygulanırsa bu semptom kendiliğinden
kaybolur, ancak `CompanyId`'si meşru olan müşteri kullanıcıları için şube kavramı zaten anlamsız
olduğundan davranışın bilinçli seçilmesi gerekir.

**Öneri:** A + B birlikte; C ayrı bir karar olarak değerlendirilsin.

---

## 9. Doğrulama sorguları

Düzeltme sonrası kontrol için:

```sql
-- Etkilenen kullanıcı sayısı (0 olmalı)
SELECT COUNT(*)
FROM ensa.UserProfile p
JOIN ensa.UserEmployment e ON e.UserId = p.UserId
JOIN ensa.UserType     t ON t.Id = e.UserTypeId
WHERE p.CompanyId IS NOT NULL AND t.StaffRole <> 5;

-- Müşteri kapsamı bozulmamış olmalı (323)
SELECT COUNT(*)
FROM ensa.UserProfile p
JOIN ensa.UserEmployment e ON e.UserId = p.UserId
JOIN ensa.UserType     t ON t.Id = e.UserTypeId
WHERE p.CompanyId IS NOT NULL AND t.StaffRole = 5;

-- Bu hesabın görmesi gereken firma sayısı (1536)
SELECT COUNT(*) FROM ensa.Company WHERE TenantId = 847 AND IsDeleted = 0;
```

Uçtan uca kontrol: bu hesapla giriş yapıp `GET /api/company` çağrısının `totalCount` değerinin
**1536** olması ve `GET /api/account/offices` çağrısının **20** şube döndürmesi beklenir
(legacy `Ofisler_T`, `KurumId = 15724` → 20 aktif şube).

Ayrıca `python tools/api-tests/api_company_scope.py` çalıştırılmalıdır: müşteri kapsamının
bozulmadığını kanıtlayan mevcut test budur.

---

## 10. Doğrulanmayanlar

1. **`StaffRole`'un müşteri ayrımı için tek başına yeterli olduğu.** 983 kullanıcının hepsinin
   gerçekten OSGB personeli olduğu tek tek incelenmedi; `UserType.StaffRole` değerine güvenildi.
   Toplu düzeltme öncesi legacy `PersonelTuru` ile çapraz kontrol önerilir.
2. **Diğer 12 mükerrer hesabın kullanıcıları.** Sonekli `UserName` ile giriş yapabilecekleri
   varsayıldı, denenmedi.
3. **Üretim veritabanı.** İnceleme yalnızca `EnsaDbDEv` (geliştirme) üzerinde yapıldı. Üretimde
   aynı göç adımı çalıştıysa aynı sonuç beklenir, ama ölçülmedi.
4. **Düzeltmenin performans etkisi.** `CompanyId` boşalınca 713 yönetici için company scope filtresi
   devre dışı kalır ve sorgular binlerce satıra açılır; sayfalama zaten var, ancak yük ölçülmedi.
5. **Diğer `ICompanyScoped` varlıkların davranışı.** Firma listesi ölçüldü; çalışan, doküman, risk
   analizi gibi 34 varlığın aynı kilitten etkilendiği filtrenin tanımından çıkarıldı, tek tek
   sorgulanmadı.

---

## 11. Uygulanan çözüm

**A + B + C birlikte uygulandı.** Sırasıyla: kodun kaynağı düzeltildi, mevcut veri onarıldı, ve
`Office` varlığının company scope ile çakışması giderildi. Mimari karar `docs/DECISIONS.md`
**ADR-042**'ta; `docs/ARCHITECTURE.md`'nin company scope bölümü de güncellendi.

### 11.1 Sınıflandırma tek bir yere alındı

`src/Ensa.DataMigrator/Infrastructure/LegacyStaffType.cs` — "bu hesap müşteri mi?" sorusunun tek
yetkili cevabı. Hem değeri yazan göç adımı hem de yanlış yazılmışı temizleyen onarım adımı aynı
fonksiyonu kullanıyor; iki kopya liste tutmak, ikisinin farklı cevap vermesinin en kısa yolu olurdu.
Karşılaştırma `OrdinalIgnoreCase` ve baş/son boşluğa toleranslı — hata yönü simetrik değil:
tanınmayan bir müşteri, sağlayıcısının bütün işyerlerini görür.

### 11.2 Göç adımı düzeltildi (Seçenek B)

`UserSplitStep.CompanyScopeAsync` artık `FirmaId` dolu olan herkese değil, yalnızca legacy
`PersonelTuru`'su müşteri olan hesaplara `CompanyId` yazıyor. Çözülemeyen bir işyeri varsa kullanıcı
kapsamsız bırakılıyor; yanlış bir id, fail-closed filtre yüzünden o kişiyi kör ederdi.

### 11.3 Mevcut veri onarıldı (Seçenek A)

`src/Ensa.DataMigrator/Steps/CompanyScopeRepairStep.cs` — varsayılan olarak yalnızca **rapor**
veriyor, yazması için `--repair-company-scope` gerekiyor (tool'un `--confirm <veritabanı>` kilidine
ek olarak). Tek sütun yazıyor, tek işlem (transaction) içinde, ön koşulları veritabanına sorarak
doğruluyor ve tutmazsa geri alıyor. Göçten sonra açılmış, legacy karşılığı olmayan profillere
dokunmuyor.

`EnsaDbDEv` üzerinde uygulandı ve doğrulandı:

```
user4785_companyId          NULL      <-- info@saglamosgb.com.tr artık kilitli değil
scoped_non_customers        0         <-- 983 hatalı kayıt temizlendi
scoped_customers            325       <-- gerçek müşteri kapsamları duruyor
tenant847_live_companies    1536      <-- bu hesabın görmesi gereken firma sayısı
tenant847_active_offices    20
```

Adımın ikinci çalıştırması `0 to clear` diyor — idempotent.

### 11.4 `Office` company scope'tan çıkarıldı (Seçenek C)

`Office` artık `ICompanyScoped` uygulamıyor. Şube kuruma aittir, müşteri işyerine değil; göç edilmiş
her şube satırında `CompanyId` zaten `NULL` ve fail-closed filtre bu satırları "sağlayıcı verisi"
sayıp firmaya bağlı kullanıcıdan gizliyordu — kendi atandığı şube dâhil. `Office.CompanyId` legacy
`COFirmaId`'nin karşılığı olarak bir *atıf* alanı hâlinde duruyor; kimin ne göreceğine karar vermiyor.
Muafiyet `ModelValidationTests` içinde adıyla listelendi, böylece "işaretlemeyi unutan varlık"
kontrolü diğer 36 varlık için çalışmaya devam ediyor.

### 11.5 Şube erişimi netleştirildi

`OfficeAccessManager`: firmaya bağlı kullanıcıya hiç şube sunulmuyor (repository'ye sorulmadan
önce), kurum/sistem yöneticisine ise atamalarının **birleşimi** olarak kurumun bütün aktif şubeleri
veriliyor. Sebebi yine göç: 766 legacy yöneticinin 678'inin hiç `KullaniciOfis_T` satırı yoktu ve
legacy onlara bütün şubeleri gösteriyordu; göç ise varsayılan şubelerini atama gibi yazmıştı. Tek
satırı "izin verilen küme" saymak, 20 şubesi olan yöneticiden 19'unu sessizce alırdı.

### 11.6 Doğrulama

| Kontrol | Sonuç |
|---|---|
| `dotnet build` | 0 uyarı, 0 hata |
| `dotnet test` | 141 test, hepsi geçti (18 yeni `LegacyStaffTypeTests` + `OfficeCompanyScopeTests`) |
| `python tools/api-tests/api_company_scope.py` | 20/20 — müşteri sınırı bozulmadı |
| `python tools/api-tests/api_office_switch.py` | 24/24 — şube bağlamı çalışıyor |
| `CompanyScopeRepairStep` (ikinci çalıştırma) | `325 scoped, 0 to clear` |
