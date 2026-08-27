# Diyagramları nasıl okursunuz

Bu klasördeki SVG'ler elle çizilmedi; **canlı veritabanından üretiliyor**. Şema değiştikçe
yeniden üretmek yeterli:

```
python tools/gen-diagram/gen_schema_diagram.py
```

## Önemli: fare ile üzerine gelin

Diyagramdaki yazılar İngilizce, çünkü onlar gerçek tablo ve sütun adları — değiştirilemezler.
**Açıklamalar Türkçe ve fareyle üzerine gelince çıkıyor.** Yalnızca `ensa-database.svg`
üzerinde **687 ipucu** var; 18 dosyanın tamamında **2252**.

Dosyayı bir tarayıcıda açın (çift tıklamak yeter). Orada koyu renkli, anında çıkan, hizalanmış
bir panel görürsünüz — beklemeden gelir, kaybolmaz ve okunacak kadar geniştir.

Tarayıcı dışında (GitHub'da, bir sayfaya `<img>` ile gömüldüğünde, ekran okuyucuda) panel
çalışmaz; orada devreye tarayıcının kendi basit ipucu girer. İki mekanizma da dosyanın içinde
duruyor: açıklama metni her iki durumda da aynı, yalnızca sunumu değişiyor. Resim
görüntüleyicide ise hiçbiri çalışmaz.

Bir tablonun üzerine geldiğinizde şunu görürsünüz:

```
CompanyEmployee  ·  Companies modülü

NE      Müşteri işyerinde çalışan personel: kimlik ve istihdam bilgileri.
NİÇİN   Muayene, eğitim, görev ataması — tüm yükümlülükler bu kişi kaydına
        bağlanır. T.C. kimlik numarası sütun bazında şifrelidir.
ESKİ    CompanyEmployee_T
KAPSAM  kiracı (TenantId) · firma (CompanyId)
BOYUT   263 527 satır · 45 sütun (36 çizili, 7 denetim, 2 kapsam)
BAĞ     4 ilişki çıkıyor · 18 ilişki geliyor
```

**NİÇİN** satırı asıl önemli olan. Diyagrama bakarak göremeyeceğiniz şeyi söyler: göç sırasında
o tablonun şeklinin neden değiştiğini. Eski sistemdeki düz sütun grubu neden satırlara ayrıldı,
hangi tablo sıfırdan oluşturuldu, hangi karar nerede verildi.

Ayrıca şunların da üzerine gelebilirsiniz:

- **modül bandının boş alanı** → modülün ne işe yaradığı
- **bir sütun** → nereye işaret ettiği, ya da polimorfikse hangi sütunun hedefi belirlediği
- **genel bakıştaki çizgiler** → iki modül arasında kaç sütunun sınırı geçtiği

## Hangi dosya

| Dosya | Ne zaman |
|---|---|
| `ensa-modules.svg` | **Önce bunu açın.** 16 modül, aralarındaki bağların kalınlığı. |
| `modules/<modül>.svg` | Bir modülü okunabilir boyutta çalışmak için. Dışarıdan işaret ettiği tablolar soluk çizilir. |
| `ensa-database.svg` | 188 tablonun tamamı tek sayfada. 2362 × 9522 piksel — tarayıcıda açıp yakınlaştırın. |

## Kutudaki işaretler

| İşaret | Anlamı |
|---|---|
| `◆` | birincil anahtar |
| `▸` | yabancı anahtar — diyagramda ok olarak çizilir |
| `▹` | **polimorfik** anahtar: sabit hedef tablosu yok, hedefi kardeş bir sütun belirler |
| `·` (adın sonunda) | boş bırakılabilir (nullable) |
| `T` / `C` başlıkta | kiracı (`TenantId`) / firma (`CompanyId`) kapsamlı |

Kutunun altındaki `created · modified · soft-delete`, o tablonun hangi denetim alanlarını
taşıdığını söyler.

## Diyagramda bilerek çizilmeyen iki şey

**`TenantId` ve `CompanyId` ok olarak çizilmiyor.** Bunlar 123 ve 37 tabloda var; tek bir kutuya
160 çizgi gitse şema görünmez olurdu. Onun yerine başlıkta `T` ve `C` rozeti duruyor.

**Denetim sütunları tekrarlanmıyor.** `CreationTime`, `CreatorId`, `LastModificationTime`,
`LastModifierId`, `DeletionTime`, `DeleterId`, `IsDeleted` — 173 tabloda birebir aynı. 173 kez
yazmak yaklaşık 1200 satır gürültü demekti; her kutunun altında tek satırda özetleniyor.

## Oklar nereden geliyor

Mimari gereği varlıklarda navigation property yok, bu yüzden veritabanı yalnızca **dokuz** gerçek
yabancı anahtar kısıtı bildiriyor — hepsi Identity ve OpenIddict'ten geliyor. Geri kalan her
ilişki, hedefinin adını taşıyan bir `int` sütunu. Üretici her `<Bir>Id` sütununu üç kaynaktan,
yetki sırasına göre çözüyor:

1. **Veritabanının gerçekten bildirdiği kısıtlar.** Varsa doğru olan odur.
2. **Property'nin XML dokümantasyonu** — adı hedefini söylemeyen bir avuç sütun için.
   `Incident.DepartmentId` kendini `FK → WorkplaceDepartment.Id` diye belgeliyor. Bunlar tek tek
   kaynaktan okundu, tahmin edilmedi.
3. **İsim kuralı** — önce birebir, sonra en uzun sonek: `PhysicianUserId` ve `ApproverUserId`
   ikisi de `User`'a düşüyor.

Sonuç **275 ilişki**. 11 sütun polimorfik olduğu için ok almıyor. 21 `...Id` sütunu ise bilerek
çözümsüz: 8'i T.C. kimlik numarası, 2'si OpenIddict metin kimliği, kalanı bu şemada karşılığı
olmayan tablolara işaret ediyor.

## Sayılar hangi veritabanından

`EnsaDbDEv` — geliştirme veritabanı. Satır sayıları göçün o anki durumunu gösterir; henüz
taşınmamış modüller (Finance, Ibys, Reports) `0 satır` görünür.
