# Kullanıcı, Kimlik ve Yetkilendirme — Mevcut Durum ve Yapılacaklar

**Revize edildi:** `Mandatory Corrections - Additions to Identity Migration Prompt.md` (§14–§28)
doğrultusunda. Bu belgedeki her madde o dosyanın kurallarına tabidir; önceki plandaki çelişen
ifadeler geçersizdir.

| | |
|---|---|
| **Tarih** | 27 Ağustos 2026 |
| **Hedef veritabanı** | `EnsaDbDEv` — yalnızca geliştirme |
| **Kaynak veritabanı** | `DemoOsgbDb` — salt okunur |
| **Legacy kaynak kodu** | `D:\EnsaProject` — salt okunur |
| **Durum** | **DURDURULDU — §28 gereği onay bekleniyor** |

## §26 uyum beyanı

Bu analiz **salt okunur** yürütüldü. Hiçbir migration çalıştırılmadı, hiçbir `UPDATE` / `DELETE` /
`INSERT` yapılmadı, hiçbir sütun düşürülmedi, hiçbir tablo değiştirilmedi, hiçbir üretim kodu
değiştirilmedi. Yalnızca `SELECT` sorguları, dosya okuma ve assembly incelemesi yapıldı.

## §28 uyum beyanı — ÇATIŞMA BİLDİRİMİ

Analiz sırasında **mevcut kod tabanının §18, §19 ve §21'i ihlal ettiği** tespit edildi. §28
gereği çözümü kendim genişletmiyorum: aşağıda raporluyorum ve onayınızı bekliyorum.

---

# BÖLÜM A — ÇATIŞMALAR (önce bunlar okunmalı)

## A1. §18 ihlali — izin kimliği `[Authorize]` içinde

§18: *"Parametresiz `[Authorize]` bir değişmezdir… `[Authorize("...")]` yasaktır… Controller/action,
kendisine erişim için gereken izin kimliğini bilmemelidir."*

**Mevcut durum bunun tam tersi:**

| Ölçüm | Değer |
|---|---|
| `[Authorize(EnsaPermissions.X.Y)]` kullanımı | **371** |
| Etkilenen dosya | **42** |
| Parametresiz `[Authorize]` | 3 |

Örnek: `[Authorize(EnsaPermissions.Company.Create)]`

Bu, §18'in açıkça yasakladığı `[Authorize("...")]` biçimidir. Kayıt yeri:
`src/Ensa.HttpApi.Host/EnsaHttpApiHostModule.cs` → `AddEnsaAuthorization`, her izin için bir
`AddPolicy(permission, …)` üretiyor.

## A2. §19 ihlali — legacy mekanizma korunmamış, yenisi uydurulmuş

§19: *"Yeni bir izin kayıt defteri, adlandırma kuralı, enum, attribute, claim veya politika
veritabanı ortaya çıkarmayın… Göç edilen uygulama, legacy projenin gözlemlenebilir yetkilendirme
semantiğini korumalıdır."*

### Legacy gerçekte nasıl çalışıyor

`D:\EnsaProject\ENSA_ISG\Algoritmalar\YetkiKontrolu.cs` okundu. Mekanizma şu:

1. Kod yalnızca `YetkiKontrolu.Authorize(kullanici, authType, parametreler)` çağırır — **izin adı
   kodda geçmez.**
2. Hedef, **çalışma anında `StackFrame` ile** belirlenir:
   - `authType == "method"` → `sf.GetMethod().ToString()` (metot imzası)
   - `authType == "page"` → `sf.GetMethod().DeclaringType.FullName` (controller tipi)
3. `Yetki_T` tablosunda **`YetkiHedefi == hedefName`** satırı aranır. **İzin kimliği veritabanında
   durur, kodda değil.**
4. Dört kapı değerlendirilir:
   - `PaketTuruYetki_T` — abonelik paketi kapsıyor mu (*"satın alınan paket dışı"*)
   - `KurumTuruYetki_T` — kurum türü kapsıyor mu (*"Kurum Türü içi kullanım dışı"*)
   - `KullaniciTypeYetki_T` **veya** `KullaniciYetki_T` (açık kullanıcı izni)
   - `KullaniciYetki_T` içinde `Yetkili = false` varsa **açık RED** — izni geri alır
5. `SerAdmin` her şeyi atlar: `if (Kullanici.SerAdmin) return;`
6. Eşleşen `Yetki_T` satırı yoksa **reddeder** (*"henüz kullanıma açılmamış"*).

### Modern uygulamada ne yapılmış

| Legacy | Modern | Değerlendirme |
|---|---|---|
| İzin kimliği `Yetki_T.YetkiHedefi`'nde (`ENSA_ISG.AcilDurumEylemPlaniListController`) | `Permission.PermissionTarget`'ta (`Ensa.Activity.Create`) | **Uydurulmuş yeni adlandırma kuralı** |
| Hedef çalışma anında StackFrame ile bulunur | Hedef attribute'a elle yazılır | **Uydurulmuş yeni mekanizma** |
| İzin kayıt defteri = veritabanı | `EnsaPermissions` sabit sınıfı (171 sabit) | **Uydurulmuş yeni kayıt defteri** |
| Kapılar sorgu ile değerlendirilir | Her izin bir `AuthorizationPolicy` | **Uydurulmuş yeni politika veritabanı** |
| — | `ensa:permission` claim'i | **Uydurulmuş yeni claim** |

§19'un yasakladığı beş şeyin **beşi de** yapılmış.

### Veri kaybı

| Tablo | Legacy | Modern | Fark |
|---|---|---|---|
| `Yetki_T` → `Permission` | **419** | **171** | −248 |
| `YetkiBaglanti_T` → `PermissionScope` | **5.069** | **0** | −5.069 |
| `KurumTuruYetki_T` → `OrganizationTypePermission` | 1.406 | 684 | −722 |
| `PaketTuruYetki_T` → `SubscriptionPlanPermission` | 1.410 | 855 | −555 |
| `KullaniciTypeYetki_T` → `UserTypePermission` | 1.360 | 540 | −820 |
| `KullaniciYetki_T` → `UserPermission` | 0 | 0 | doğru |
| `YetkiKisit_T` → `PermissionRestriction` | — | 0 | doğrulanmalı |

Legacy izin modeli **sadık şekilde göç ettirilmemiş**; yerine daha küçük ve elle yazılmış bir
model konmuş. §19 bunu açıkça yasaklıyor.

## A3. §21 ihlali — izinler OpenIddict token'ında

§21: *"Yalnızca izin sistemini sorgulamamak için iş izin verilerini OpenIddict token'larına
koymayın."*

Mevcut durumda `/connect/token`, kullanıcının **tüm izinlerini** `ensa:permission` claim'i olarak
token'a yazıyor (`AuthorizationController.cs`), yetkilendirme de bu claim'i okuyor
(`policy.RequireClaim(EnsaClaimTypes.Permission, permission)`). Bu, §21'in tarif ettiği
yasaklanmış tasarımın kendisi.

## A4. §14 / §22 çatışması — `CompanyId` bağımlılığı

§14: *"`CompanyId`'yi veya başka bir alana özgü alanı Identity User'a açık onay olmadan
eklemeyin."*
§22: *"Mevcut kimlik doğrulama veya yetkilendirme başka bir User seviyesinde tenant/company alanı
gerektiriyorsa, bağımlılığı raporlayın. Sessizce korumayın."*

**Bağımlılığı raporluyorum — sessizce korumuyorum:**

| Bağımlılık | Yer |
|---|---|
| Token'a `ensa:companyId` claim'i yazılıyor | `AuthorizationController.cs:399` |
| `ICurrentUser.CompanyId` bu claim'den okunuyor | `HttpContextCurrentUser.cs:43` |
| **38 entity** `ICompanyScoped` uyguluyor ve global sorgu filtresi `ICurrentUser.CompanyId`'ye dayanıyor | `EnsaDbContext` |
| Filtre **kapalı başarısız** olur (ADR-034): `CompanyId` çözülemezse firma kullanıcısı veri göremez | — |

**Sonuç:** `User.CompanyId` kaldırılırsa firma kullanıcısı izolasyonu çalışmaz. Ancak §14 bunu
onaysız tutmayı yasaklıyor. **Karar sizin** — Bölüm D, Karar 1.

---

# BÖLÜM B — §16: GERÇEK IDENTITY MODELİ (ölçüldü, varsayılmadı)

§16 gereği çözümün gerçekten referans verdiği sürüm incelendi:

| | |
|---|---|
| Paket | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` |
| Çözülen sürüm | **10.0.11** |
| Hedef çatı | `net10.0` |
| İncelenen assembly | `Microsoft.Extensions.Identity.Stores.dll` 10.0.11 |

`IdentityUser<TKey>`'in bu sürümdeki **gerçek** özellikleri — assembly'den doğrulandı, 15/15:

```
Id                    UserName              NormalizedUserName
Email                 NormalizedEmail       EmailConfirmed
PasswordHash          SecurityStamp         ConcurrencyStamp
PhoneNumber           PhoneNumberConfirmed  TwoFactorEnabled
LockoutEnd            LockoutEnabled        AccessFailedCount
```

Mevcut `User` tablosu **49 sütun** taşıyor: 15'i Identity'nin, **34'ü Ensa eklentisi**.

## §15 — standart Identity altyapısının tamamı

Yedi tablonun tamamı mevcut ve korunacak. **Satır sayısı sıfır diye hiçbiri kaldırılmayacak** (§15):

| Tablo | Satır | Karar |
|---|---|---|
| `User` | 3.886 | Standart şekle indirilecek |
| `Role` | 7 | KORUNACAK |
| `UserRole` | 1 | KORUNACAK |
| `UserClaim` | 0 | **KORUNACAK** — boş olması kaldırma gerekçesi değil |
| `UserLogin` | 0 | **KORUNACAK** |
| `UserToken` | 0 | **KORUNACAK** |
| `RoleClaim` | 0 | **KORUNACAK** |

## §0 — OpenIddict'in kullanıcı tablosu yoktur

Önceki talebinize cevaben, kanıtla: `OpenIddict.EntityFrameworkCore.Models` 7.6.0 assembly'sinin
içindeki **tüm** entity tipleri `…Application`, `…Authorization`, `…Scope`, `…Token`'dır. DLL
içinde "User" geçen tek bir tip yoktur. Yönetici arayüzleri de aynı dörtlüdür. Bu, §14'ün de
söylediği şeydir: *"OpenIddict'in User entity/tablosu yoktur, dolayısıyla User tablosu için bir
adlandırma kuralı tanımlamaz."* Kullanıcı deposu **yalnızca ASP.NET Core Identity'nindir.**

OpenIddict'in dört tablosu şema olarak **zaten standart** (`builder.UseOpenIddict<int>()`, özel
yapılandırma yok, fazladan sütun yok). Yalnız içeriği eksik:

| Tablo | Satır | Durum |
|---|---|---|
| `OpenIddictApplications` | 0 | İstemci seed edilmeli |
| `OpenIddictScopes` | 0 | Kapsamlar seed edilmeli |
| `OpenIddictAuthorizations` | 15 | Test artığı |
| `OpenIddictTokens` | 51 | Test artığı |

---

# BÖLÜM C — MEVCUT DURUM (veri)

## C1. Parolalar aktarılmamış

| Taraf | Ölçüm | Değer |
|---|---|---|
| `EnsaDbDEv` | Toplam kullanıcı | 3.886 |
| `EnsaDbDEv` | `PasswordHash` dolu | **8** |
| `EnsaDbDEv` | `PasswordHash` NULL | **3.878** |
| `DemoOsgbDb` | `Kullanici_T` toplam | 3.878 |
| `DemoOsgbDb` | `Sifre` dolu | 3.874 |

Legacy `Sifre` biçimi:

| Uzunluk | Adet | Yorum |
|---|---|---|
| 128 | 3.867 | LegacyCrypt, bir kez şifreli |
| 300 | 4 | İki kez şifreli |
| 8–9 | 3 | Düz metin |

Çözülebilirlik kanıtı: aynı tablodaki `TCKimlikNo` birebir aynı profile sahip (128/300/11/0) ve
`tenancy` adımında `LegacyCrypt` ile çözülüp 240.051 değer 11 haneli kimlik numarası olarak geri
okundu. Şema: Rijndael 256-bit blok, CBC/PKCS7, sabit tuz üzerinde PBKDF2, 1000 tur.

## C2. `User` üzerindeki Ensa alanları

| Sütun | Dolu | Sütun | Dolu |
|---|---|---|---|
| `OfficeId` | 3.558 | `MedulaUserName` | 297 |
| `MonthlyWorkDurationMinutes` | 1.542 | `MedulaPassword` | 276 |
| `HireDate` | 2.561 | `BranchCode` | 300 |
| `GrossSalary` | 60 | `Address` | 1.302 |
| `OrganizationAdmin` | 1.053 | `Color` | 2 |
| `OfficeAdmin` | 97 | `PhotoDocumentId` | 0 |
| `SystemAdministrator` | 2 | `PermissionGroupId` | 0 |

## C3. Taşınmamış legacy verisi

| Legacy | Satır | Modern hedef | Satır |
|---|---|---|---|
| `KullaniciOfis_T` | **1.949** | `UserOffice` | **0** |
| `BazalKullanici_T` | **59** | `StaffCostBaseline` | **0** |
| `Kullanici_T.Resim` | 46 | `PhotoDocumentId` | 0 |
| `Kullanici_T.PersonelTuru` | 3.706 | *(bağ yok — `User`'da `UserTypeId` yok)* | — |

---

# BÖLÜM D — §17: ADMİN BAYRAKLARI (tek tek analiz, otomatik dönüşüm YOK)

§17: *"Legacy yönetici bayrakları izin değil de gerçek rol ifade ediyorsa, her birini ayrı ayrı
analiz edin ve eşlemeyi göç öncesi raporlayın. Her legacy yetkilendirme bayrağını otomatik olarak
Identity Role'e çevirmeyin."*

**Önceki plandaki "üçünü de role çevir" önerisi §17'ye aykırıydı; geri çekiyorum.** Tek tek analiz:

| Bayrak | Legacy adet | Legacy koddaki kullanımı | Değerlendirme |
|---|---|---|---|
| `SerAdmin` | **1** | `YetkiKontrolu.cs`: `if (Kullanici.SerAdmin) return;` — **tüm yetkilendirmeyi atlar** | **Gerçek rol.** İzin değil, denetimi tamamen baypas eden bir süper-yönetici sıfatı. Identity Role'e uygun aday. |
| `Admin` | **1.052** | Yetkilendirme kontrolünde **hiç geçmiyor**. `KullaniciIslemleri.cs`, `KullaniciGiris.cs`, `DefaultController.cs`, `FirmaListController.cs`, `FirmaEkleController.cs` içinde **iş mantığı dallanması** olarak kullanılıyor | **Rol olduğu kanıtlanmadı.** Yetkilendirme kapılarının hiçbirine girmiyor. Role çevirmeden önce bu beş dosyadaki davranış tek tek çıkarılmalı. |
| `OfisAdmin` | **97** | Yetkilendirme kontrolünde **hiç geçmiyor**. `OfisIslemleri.cs` içinde iş mantığı | **Rol olduğu kanıtlanmadı.** Aynı gerekçe. |

**Öneri:** yalnızca `SerAdmin` için rol dönüşümü değerlendirilsin; `Admin` ve `OfisAdmin` bu
raporun kapsamında **dönüştürülmesin**, davranışları ayrı bir analizle çıkarılsın.

---

# BÖLÜM E — §25: SÜTUN DÜŞÜRME SINIFLANDIRMASI

§25: *"Bir sütun yalnızca `MOVED_AND_VERIFIED` veya `CONFIRMED_UNUSED` olarak sınıflandırıldıktan
sonra düşürülebilir… Bir alanı yalnızca şu anda NULL/sıfır içeriyor diye ölü saymayın."*

**Önceki plandaki "`PermissionGroupId` ölü sütun, kaldırılır" ifadesi §25'i ihlal ediyordu; geri
çekiyorum.** Doğru sınıflandırma:

| Sütun | Sınıf | Gerekçe |
|---|---|---|
| `PermissionGroupId` | **SINIFLANDIRILAMADI** | Modern kodda **15 referans** (DTO ×2, entity, EF config index, migration). Legacy kodda `YetkiGrubuId` **kullanılıyor**: `KullaniciIslemleri.cs`, `KullaniciGiris.cs`. Sırf 0 dolu diye ölü sayılamaz. |
| Diğer taşınacak sütunlar | Hedef: `MOVED_AND_VERIFIED` | Hedef satır sayıları ve değerleri doğrulanmadan düşürülmeyecek |

`PermissionGroupId` için düşürme kararı, `KullaniciGiris.cs` içindeki kullanımın ne yaptığı
çıkarılana kadar **verilmeyecek**.

---

# BÖLÜM F — §24: KİMLİK VE YABANCI ANAHTAR KORUMASI

§24: *"Göç, mevcut User ID'lerini mümkün olan her yerde korumalıdır… Yetim yabancı anahtar kabul
edilemez."*

`User.Id`'ye işaret eden **439 sütun** tespit edildi (`CreatorId`, `LastModifierId`, `DeleterId`
denetim sütunları + açık `UserId` sütunları: `AssignedSpecialist.UserId`,
`Archive.PreviousAddedByUserId`, `UserOffice.UserId`, …).

**Karar:** `User.Id` **değiştirilmeyecek.** Tablo yeniden adlandırılmayacak, birincil anahtar
yeniden üretilmeyecek. Yeni tablolar `UserId` ile mevcut kimliğe bağlanacak. Yıkıcı adımdan önce
`eski User.Id == yeni User.Id` doğrulaması yapılacak ve yetim yabancı anahtar taraması
çalıştırılacak.

---

# BÖLÜM G — §23: GÜVENLİK HASSASİYETİ OLAN ALANLAR

§23 gereği her alan için beş soru:

## G1. Legacy parola (`Kullanici_T.Sifre`)

| Soru | Cevap |
|---|---|
| Hâlâ gerekli mi? | Evet — kullanıcıların giriş yapabilmesi için |
| Nerede saklanacak? | `User.PasswordHash` (Identity'nin kendi sütunu) |
| Şifreleme/hash gerekli mi? | **Hash zorunlu** — tek yönlü |
| Mevcut koruma kabul edilebilir mi? | **HAYIR.** Geri çözülebilir şifreleme; DB'ye erişen herkes okuyabiliyordu |
| Göç korumayı değiştiriyor mu? | **Evet, iyileştiriyor:** geri çözülebilir şifreleme → PBKDF2-HMAC-SHA256 tek yönlü hash |

## G2. Medula parolası (`MedulaSifre`)

| Soru | Cevap |
|---|---|
| Hâlâ gerekli mi? | **DOĞRULANMALI** — e-reçete entegrasyonu hâlâ Medula'ya bağlanıyor mu? |
| Nerede saklanacak? | Domain tarafında `UserMedulaCredential` (Identity'de değil) |
| Şifreleme gerekli mi? | **Evet** — dış sisteme gönderilmesi gerektiği için hash değil, geri çözülebilir şifreleme |
| Mevcut koruma kabul edilebilir mi? | Sütun bazında AES şifreleme var; anahtar yönetimi ile birlikte kabul edilebilir |
| Göç korumayı değiştiriyor mu? | Hayır — aynı `EncryptedStringConverter` kullanılacak |

## G3. `NationalId` (T.C. kimlik numarası)

| Soru | Cevap |
|---|---|
| Hâlâ gerekli mi? | Evet — İSG-KATİP ve e-reçete eşleşmesi buna dayanıyor |
| Nerede saklanacak? | Domain tarafında (`UserProfile`) — Identity User'a ait değil |
| Şifreleme gerekli mi? | Evet — kişisel veri |
| Mevcut koruma kabul edilebilir mi? | Evet — sütun bazında şifreli |
| Göç korumayı değiştiriyor mu? | Hayır |

## G4. `SecurityStamp` / token verisi

Identity ve OpenIddict'in kendi alanları. **Elle taşınmayacak, kopyalanmayacak;** parola
yazıldığında `SecurityStamp` framework tarafından yeniden üretilecek (§22: framework
implementasyonları kullanılacak).

**Değişmez kural (§23):** çözülmüş kimlik bilgisi veya düz metin parola **hiçbir yere
loglanmayacak**, diske yazılmayacak, ekrana basılmayacak.

---

# BÖLÜM H — §27 HEDEF MİMARİ VE REVİZE PLAN

## H1. Sorumluluk sınırı (§27)

```
ASP.NET Core Identity        →  User (+ TenantId), Role, UserRole,
                                UserClaim, RoleClaim, UserLogin, UserToken
OpenIddict                   →  Applications, Authorizations, Scopes, Tokens
Legacy İzin Modeli           →  iş yetkilendirmesinin TEK otoritesi
Domain                       →  profil, istihdam, ofis, Medula, diğer
```

Dördüncü bir kimlik/yetkilendirme mimarisi **olmayacak** (§27). Seçim gerektiğinde ASP.NET Core
Identity / OpenIddict **resmî kullanım biçimi** tercih edilecek (§28).

## H2. `User` hedef şekli

**Kalacak 15 sütun** — Identity 10.0.11'in `IdentityUser<int>` tipinin ölçülmüş hâli — **artı
`TenantId`** (§14'ün onayladığı tek eklenti).

`CompanyId`'nin durumu **Karar 1**'e bağlı (Bölüm A4).

**Taşınacaklar:**

| Sütun | Hedef |
|---|---|
| `Name`, `LastName`, `NationalId`, `Address`, `CityId`, `DistrictId`, `PhotoDocumentId`, `Color`, `IsActive`, `MustChangePassword`, `ContractApproved` | `UserProfile` (yeni) |
| `HireDate`, `TerminationDate`, `GrossSalary`, `PartTime`, `StaffRole`→`UserTypeId` | `UserEmployment` (yeni) |
| `MedulaUserName`, `MedulaPassword`, `BranchCode` | `UserMedulaCredential` (yeni) |
| `OfficeId`, `MonthlyWorkDurationMinutes` | `UserOffice` (mevcut) |
| `OrganizationAdmin`, `OfficeAdmin`, `SystemAdministrator` | **BEKLEMEDE** — §17, Bölüm D |
| `PermissionGroupId` | **BEKLEMEDE** — §25, Bölüm E |
| Denetim sütunları | `UserProfile` |

## H3. Yetkilendirmenin §18–§21'e uygun hâli

Legacy semantiği koruyan, `[Authorize]` parametresiz kalan tasarım:

```
[Authorize]                    ← parametresiz, izin kimliği YOK
      ↓
kimliği doğrulanmış kullanıcı
      ↓
merkezî yetkilendirme (ASP.NET Core authorization uzantı noktası)
      ↓
çalışılan endpoint → Permission.PermissionTarget eşlemesi   ← legacy Yetki_T.YetkiHedefi karşılığı
      ↓
dört kapı: PaketTuru ∧ KurumTuru ∧ (KullaniciTipi ∨ KullaniciIzni) ∧ ¬açıkRed
      ↓
ALLOW / FORBID
```

Bunun için gerekenler:

1. **371 attribute'tan izin kimliği çıkarılacak**, hepsi parametresiz `[Authorize]` olacak.
2. **`EnsaPermissions` sabit sınıfı ve politika üretimi kaldırılacak** (§19'un yasakladığı kayıt
   defteri ve politika veritabanı).
3. **`ensa:permission` claim'i token'dan çıkarılacak** (§21); yetkilendirme izin modelini
   sorgulayacak.
4. **Legacy izin verisi sadık şekilde yeniden göç ettirilecek:** 419 `Yetki_T`, 5.069
   `YetkiBaglanti_T` ve üç kapı tablosu; `PermissionTarget` legacy hedefini taşıyacak.
5. Endpoint → hedef çözümü legacy'nin yaptığı işi karşılayacak biçimde kurulacak (legacy
   `StackFrame` kullanıyor; ASP.NET Core'da bunun karşılığı endpoint meta verisidir —
   **bu noktada semantik farkı Karar 4'te soruyorum**).

---

# BÖLÜM I — ONAYINIZI BEKLEYEN KARARLAR

§28 gereği hiçbirini kendim çözmüyorum.

| # | Konu | Dayanak | Önerim |
|---|---|---|---|
| **1** | `User.CompanyId` kalsın mı? Kalmazsa 38 `ICompanyScoped` entity'nin firma izolasyonu ve `ensa:companyId` claim'i çalışmaz. | §14, §22 | **Kalsın** — ama §14 açık onay istiyor, o yüzden soruyorum. Alternatif: `UserProfile.CompanyId` + kimlik çözümünde ek sorgu. |
| **2** | 371 `[Authorize(...)]` parametresize dönsün mü? Bu, HttpApi katmanının tamamına dokunur. | §18 | **Dönsün** — §18 bir değişmez, mevcut kod onu ihlal ediyor. |
| **3** | Legacy izin modeli (419 izin + 5.069 bağlantı) sadık şekilde yeniden göç ettirilsin mi? Mevcut 171 uydurulmuş izin atılacak. | §19 | **Edilsin** — mevcut model legacy semantiğini korumuyor. |
| **4** | Legacy `StackFrame` tabanlı hedef çözümü, ASP.NET Core'da endpoint meta verisi ile karşılanacak. Bu, aynı sonucu veren farklı bir teknik. **Semantik fark riski var.** | §19, §20 | Onayınızı istiyorum; §19 "temiz şekilde yeniden üretilemiyorsa DUR ve bildir" diyor — bildiriyorum. |
| **5** | `SerAdmin` (1 kullanıcı) Identity Role'e dönsün mü? | §17 | **Dönsün** — legacy'de tüm denetimi baypas eden gerçek bir rol. |
| **6** | `Admin` (1.052) ve `OfisAdmin` (97) bu kapsamda **dönüştürülmesin**, ayrı analiz edilsin. | §17 | **Dönüştürülmesin** — yetkilendirme kapılarında hiç geçmiyorlar. |
| **7** | `PermissionGroupId` düşürme kararı, `KullaniciGiris.cs` analizine kadar ertelensin. | §25 | **Ertelensin** |
| **8** | Medula entegrasyonu hâlâ kullanılıyor mu? Kullanılmıyorsa 276 parola taşınmaz, silinir. | §23 | Bilgi bekliyorum |
| **9** | `Name` / `LastName` `UserProfile`'a taşınsın mı? Neredeyse her listede fazladan `join` demek. | §14 | **Taşınsın** — §14 yalnızca `TenantId`'yi onaylıyor |

---

# BÖLÜM J — ÖNERİLEN SIRA (onay sonrası)

| Sıra | İş | Bağımlılık |
|---|---|---|
| 1 | `PasswordStep` — parola göçü (çöz → hash → yaz) | Yok |
| 2 | OpenIddict istemci/kapsam seed'i + test artığı temizliği | Yok |
| 3 | Legacy izin modelinin sadık göçü (419 + 5.069 + kapılar) | Karar 3 |
| 4 | Yetkilendirmenin §18–§21'e uyarlanması, 371 attribute'un sadeleştirilmesi | Karar 2, 4 |
| 5 | `UserProfile` / `UserEmployment` / `UserMedulaCredential` + **ekleyen** migration | Karar 1, 9 |
| 6 | Veri taşıma + `KullaniciOfis_T` (1.949), `BazalKullanici_T` (59), fotoğraflar, kullanıcı tipleri | 5 |
| 7 | Doğrulama: satır sayıları, `eski Id == yeni Id`, yetim FK taraması | 6 |
| 8 | Eski sütunları **düşüren** ikinci migration — yalnızca `MOVED_AND_VERIFIED` / `CONFIRMED_UNUSED` olanlar | §25 |

---

# BÖLÜM K — DOĞRULAMA PLANI

| Ne | Nasıl |
|---|---|
| Identity sözleşmesi | `User` tablosunun 15 Identity sütunu + `TenantId` taşıdığı; tip `IdentityUser<int>`'ten türediği |
| §15 tamlığı | Yedi Identity tablosunun da var olduğu |
| OpenIddict | Dört tablonun sütunlarının OpenIddict 7.6 şemasıyla birebir aynı kaldığı |
| §18 değişmezi | Kod tabanında izin kimliği taşıyan `[Authorize]` sayısının **0** olduğu |
| §21 ayrımı | Token'da `ensa:permission` claim'inin bulunmadığı |
| §19 sadakati | `Permission` 419, `PermissionScope` 5.069 satır; `PermissionTarget` legacy hedefini taşıyor |
| §24 kimlik | `eski User.Id == yeni User.Id`; 439 referans için yetim FK taraması → 0 |
| Parola | Örneklem üzerinde `VerifyHashedPassword = Success` (parola yazdırılmadan) |
| Yetkilendirme davranışı | `api_authorization.py`, `api_company_scope.py`, `api_test.py`, `frontend_calls.py` |
| Giriş | Örnek bir legacy kullanıcının gerçekten giriş yapabildiği |

---

**§26 gereği bu aşama salt okunur tamamlandı. §28 gereği Bölüm I'deki dokuz karar onaylanmadan
hiçbir kod yazılmayacak, hiçbir veri değiştirilmeyecek.**
