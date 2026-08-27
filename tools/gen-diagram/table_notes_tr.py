# -*- coding: utf-8 -*-
"""Turkish hover notes for every table in the diagrams.

The repository is written in English, and stays that way. These notes are the one exception:
they are read by the people who commissioned the migration, in the tooltip of the diagram, and
they exist to answer two questions -- *what is this table* and *why is it shaped like this*.
So they are Turkish on purpose, and they are prose rather than a restatement of the column list.

Each entry is ``"Table": (what, why)``:

* **what**  -- one sentence: what a row in this table is.
* **why**   -- the decision worth knowing. Where the migration changed the shape of the legacy
  data (a flat column group turned into rows, a new table with no legacy counterpart, a tenancy
  call), that is what this says, because that is what a reader cannot recover from the diagram.
  ``None`` where the table is a plain one-to-one carry-over and there is nothing to explain.

The legacy table name is **not** stored here: the generator reads it from the entity's own XML
documentation at run time, so it cannot drift out of step with the code.
"""

NOTES: dict[str, tuple[str, str | None]] = {
    # -- Tenancy ------------------------------------------------------------------------
    "Organization": (
        "Kiracının kök kaydı: sistemi kullanan OSGB ya da kendi İSG birimini işleten işletme.",
        "YENİ VARLIK. Eski sistemde çok kiracılılık kavramı hiç yoktu; her kurulum tek bir "
        "firmaya aitti. Tüm veri artık bu kayda bağlı ve kiracılar birbirinin verisini göremez.",
    ),
    "Office": (
        "Kurumun fiziksel ofisi veya şubesi.",
        "Kullanıcılar ve iş planları ofis bazında yönetilir; personel maliyeti ve İSG-KATİP "
        "dakika kapasitesi de ofise göre hesaplanır.",
    ),
    "OrganizationType": (
        "Kurum türü referansı: OSGB, işletme, bakanlık gibi.",
        "İzin kataloğunda ZORUNLU KAPIDIR — bir yetkinin kuruma açılabilmesi için önce kurum "
        "türüne açılmış olması gerekir.",
    ),
    "OrganizationContract": (
        "İmzalanmış abonelik sözleşmesi — aday müşterinin kiracıya dönüştüğü resmî kayıt.",
        "Host seviyesinde tutulur: sözleşme kiracıdan önce vardır, dolayısıyla bir kiracıya ait "
        "olamaz.",
    ),
    "ProspectOrganization": (
        "Aday müşteri — henüz kiracıya dönüşmemiş satış fırsatı.",
        "Host seviyesinde bir CRM kaydı. Eski adı CustomerPackage_T idi; isim paket satışını "
        "ima ediyordu, oysa kayıt aslında adayın kendisidir.",
    ),
    "SubscriptionPlan": (
        "Abonelik paketi referansı.",
        "Host tablosu — tüm kiracılar aynı paket listesini paylaşır, bu yüzden TenantId taşımaz.",
    ),
    "SalesRep": (
        "Aday ve sözleşmeleri takip eden satış temsilcisi.",
        "Eskiden ayrı bir kişi tablosuydu; artık host kullanıcısına bağlanır, böylece temsilci "
        "aynı zamanda sisteme giren bir kullanıcıdır.",
    ),
    "SalesRepScreenField": (
        "Satış temsilcisi ekranlarında hangi alanın, hangi başlıkla, nerede görüneceğinin ayarı.",
        None,
    ),

    # -- Membership ---------------------------------------------------------------------
    "User": (
        "Sisteme giren kullanıcı: İSG uzmanı, işyeri hekimi, ofis personeli veya firma kullanıcısı.",
        "ASP.NET Core Identity tablosu, Ensa alanlarıyla genişletildi. Parola hash'i ve T.C. "
        "kimlik numarası gibi alanlar sütun bazında şifrelidir.",
    ),
    "Role": ("Kullanıcı rolü.", "ASP.NET Core Identity'nin kendi tablosu."),
    "UserRole": ("Kullanıcı–rol bağlantısı.", "ASP.NET Core Identity'nin kendi tablosu."),
    "UserClaim": ("Kullanıcıya doğrudan iliştirilmiş claim.", "ASP.NET Core Identity'nin kendi tablosu."),
    "RoleClaim": ("Role iliştirilmiş claim.", "ASP.NET Core Identity'nin kendi tablosu."),
    "UserLogin": ("Dış sağlayıcı ile giriş kaydı.", "ASP.NET Core Identity'nin kendi tablosu."),
    "UserToken": ("Kullanıcıya ait token.", "ASP.NET Core Identity'nin kendi tablosu."),
    "UserType": (
        "Kullanıcı tipi referansı: İSG uzmanı, işyeri hekimi, ofis personeli...",
        "Yetkilendirmenin belkemiği — varsayılan izinler bu tipe göre verilir.",
    ),
    "UserOffice": (
        "Kullanıcının hangi ofis(ler)de, ayda kaç dakika çalıştığı.",
        "Çoka-çok bağlantı: bir kullanıcı birden fazla ofiste, farklı sürelerle görev alabilir.",
    ),
    "Permission": (
        "İzin tanımı — sayfa veya metot düzeyinde bir erişim hakkı.",
        "Host referans tablosu: izin kataloğu tüm kiracılar için ortaktır, kiracı yalnızca "
        "kataloğdaki izinleri dağıtabilir.",
    ),
    "UserPermission": (
        "Tek bir kullanıcıya açıkça VERİLEN ya da REDDEDİLEN izin.",
        "En son sözü söyleyen katman: buradaki bir RED, tipten veya paketten gelen izni ezer.",
    ),
    "UserTypePermission": (
        "Bir kullanıcı tipine varsayılan olarak verilen izin.",
        "Eski bağlantı UserTypePermission üzerinden dolaylıydı; doğrudan bağlandı.",
    ),
    "OrganizationTypePermission": (
        "Bir kurum türüne açılmış izin.",
        "ZORUNLU KAPI: kurum türüne açılmamış bir izin, kullanıcıya verilse bile çalışmaz.",
    ),
    "SubscriptionPlanPermission": (
        "Bir abonelik paketinin kapsadığı izin.",
        "ZORUNLU KAPI: paket kapsamamışsa izin çalışmaz. Yetki üç kapıdan da geçmek zorundadır.",
    ),
    "PermissionRestriction": (
        "Bir izni belirli kullanıcı tipleriyle sınırlayan kısıt.",
        "Eski tabloda kiracı sütunu OrganizationId adını taşıyordu; TenantId'ye normalize edildi.",
    ),
    "PermissionScope": (
        "Bir izni modül, menü, hesap gibi bir nesneye bağlar.",
        "LinkTargetId polimorfiktir: hangi tabloya baktığı LinkType sütununa göre değişir, bu "
        "yüzden diyagramda ok çizilmez.",
    ),
    "StaffCostBaseline": (
        "Bir kullanıcının belirli bir dönemdeki personel maliyeti ve İSG-KATİP dakika kapasitesi.",
        "Anlık görüntü kaydıdır: geçmiş dönemin maliyeti sonradan değişmesin diye o günkü "
        "değerler dondurulur.",
    ),

    # -- Identity -----------------------------------------------------------------------
    "OpenIddictApplications": (
        "OAuth/OpenID istemci tanımı.",
        "OpenIddict 7'nin kendi tablosu. Veritabanının bildirdiği dokuz gerçek yabancı anahtar "
        "kısıtından bir kısmı buradan gelir.",
    ),
    "OpenIddictAuthorizations": ("Verilmiş yetkilendirme kaydı.", "OpenIddict 7'nin kendi tablosu."),
    "OpenIddictTokens": ("Erişim ve yenileme token'ları.", "OpenIddict 7'nin kendi tablosu."),
    "OpenIddictScopes": ("Tanımlı OAuth kapsamları.", "OpenIddict 7'nin kendi tablosu."),

    # -- Menus --------------------------------------------------------------------------
    "Menu": (
        "Menü tanımı — kullanıcı tipi, kurum türü ve paket kombinasyonuna sunulan ağacın kökü.",
        "YENİ VARLIK. Eskiden menünün kökü diye bir kayıt yoktu; hangi menünün kime gittiği "
        "kodun içine gömülüydü.",
    ),
    "MenuNode": (
        "Bir menü girdisinin bir menü içindeki hiyerarşik yerleşimi.",
        "Aynı girdi farklı menülerde farklı yerlerde durabildiği için yerleşim, girdinin "
        "kendisinden ayrıldı.",
    ),
    "MenuItem": (
        "Yeniden kullanılabilir menü girdisi kataloğu: başlık, ikon ve URL.",
        "Aynı sayfa birden çok menüde görünebilsin diye tanım tek yerde tutulur.",
    ),
    "MenuElement": (
        "Serbest menü elemanı: kendi metnini, ikonunu ve URL'sini taşıyan, kataloğa bağlı olmayan düğüm.",
        None,
    ),
    "MenuType": ("Menü yerleşim tipi: yan menü, üst menü, hızlı erişim.", "Host referans tablosu."),
    "MenuPage": (
        "Sayfa–menü eşlemesi: bir URL açıldığında hangi menünün, hangi bölgede gösterileceği.",
        None,
    ),
    "UserMenuOverride": (
        "Kullanıcıya özel menü değişikliği: bir girdiyi EK olarak gösterir ya da GİZLER.",
        None,
    ),
    "Module": (
        "Hiyerarşik uygulama modülü: Eğitim, Risk Değerlendirme, Sağlık Gözetimi...",
        "Hem menünün hem de firma bazlı modül yetkilendirmesinin dayandığı ağaç.",
    ),
    "CompanyModule": (
        "Bir firmaya açılmış modül — firma bazlı modül hakkı.",
        None,
    ),
    "Icon": ("İkon kataloğu kaydı.", None),
    "IconLibrary": ("İkon kütüphanesi: Font Awesome, Metronic, Line Awesome.", "Host referans tablosu."),

    # -- Companies ----------------------------------------------------------------------
    "Company": (
        "Müşteri işyeri (firma). Alanın çekirdek kaydıdır.",
        "Neredeyse her İSG süreci buraya bağlanır. ADR-034 gereği firma kullanıcısı yalnızca "
        "kendi firmasının satırlarını görür; filtre KAPALI BAŞARISIZ olur, yani CompanyId'si "
        "olmayan kayıt firma kullanıcısından gizlenir.",
    ),
    "CompanyEmployee": (
        "Müşteri işyerinde çalışan personel: kimlik ve istihdam bilgileri.",
        "Muayene, eğitim, görev ataması — tüm yükümlülükler bu kişi kaydına bağlanır. T.C. "
        "kimlik numarası sütun bazında şifrelidir.",
    ),
    "WorkplaceDepartment": (
        'İşyerinin fiziksel/organizasyonel bölümü ("kaynakhane", "idari bina").',
        "Eskiden personelin çalıştığı bölüm serbest metindi; normalize edilip gerçek bir kayda "
        "dönüştürüldü. Risk değerlendirmesi ve saha gözlemi buraya bağlanır.",
    ),
    "DepartmentDocument": (
        "İşyeri bölümüne ait belge: ölçüm raporu, muayene sertifikası, ruhsat.",
        None,
    ),
    "AssignedSpecialist": (
        "Firmaya atanmış hizmet personeli: İSG uzmanı, işyeri hekimi veya DSP.",
        "İSG-KATİP'e bildirilen atamanın sistemdeki karşılığı.",
    ),
    "AssignedSpecialistDocument": (
        "Atama evrakı: sözleşme, İSG-KATİP çıktısı ve benzeri.",
        None,
    ),
    "CompanyActivity": (
        "Firmanın hizmet kapsamına dahil edilmiş aktivite/doküman tanımı.",
        "NORMALİZASYON: eski sistem aktiviteyi serbest metinle tekrarlıyordu; katalog kaydına "
        "bağlandı.",
    ),
    "CompanyEmployeeDocument": (
        "Personel ile belge arasındaki bağ: eğitim katılım belgesi, sertifika, sağlık raporu.",
        None,
    ),
    "CompanyEmployeeDuty": (
        "Personele verilmiş İSG görevi: ilk yardımcı, söndürme ekibi, çalışan temsilcisi, kurul üyesi.",
        None,
    ),
    "CompanyEmployeeDutyDocument": (
        "Görevi belgeleyen evrak: ilk yardımcı sertifikası, görevlendirme yazısı.",
        None,
    ),
    "EmployeeHealthInfo": (
        "Personelin değişmeyen sağlık bilgisi (kan grubu, alerji).",
        "NORMALİZASYON: eskiden personel tablosundaki düz sütunlardı; 1-1 ayrı kayda taşındı.",
    ),
    "EmployeeFamilyHistory": (
        "Personelin aile öyküsü — ailede bilinen hastalıklar.",
        "NORMALİZASYON: eski FamilyHistory sütun grubunun yerine geçen satırlar. Artık kaç "
        "hastalık girileceğinin sınırı yok.",
    ),
    "EmployeeImmunization": (
        "Personelin aşı kaydı.",
        "NORMALİZASYON: eskiden Tetanoz, Hepatit, Grip diye ayrı ayrı sütunlardı; her aşı bir "
        "satır oldu, yeni aşı eklemek şema değişikliği gerektirmiyor.",
    ),
    "EmployeeWorkHistory": (
        "Personelin önceki çalışma geçmişi.",
        "NORMALİZASYON: eski PreviousIsIskolu1/2/3... tekrarlı sütunlarının yerine geçti.",
    ),
    "CompanyCheck": ("Firmanın belirli bir aya ait kontrol listesi başlığı.", None),
    "CompanyCheckLine": ("Aylık kontrol listesindeki tek bir maddenin sonucu.", None),
    "ControlItem": ("Kontrol listesi maddesinin tanımı.", "Kurum bazında tanımlanır."),
    "CompanyComplianceSummary": (
        "Firmanın bekleyen yükümlülüklerinin sayısal özeti.",
        "DENORMALİZE tablodur — bilerek. Uyum panelinin her açılışta milyonlarca satır "
        "taraması yerine önceden hesaplanmış sayıları okuması için var.",
    ),
    "CompanyLedgerEntry": (
        "Firmaya ait cari hareket (borç veya alacak).",
        "NORMALİZASYON: kaynak modül eskiden serbest metindi, enum'a çevrildi. OperationId "
        "polimorfiktir — hangi tabloya baktığı kaynak modüle göre değişir.",
    ),
    "CompanyTag": ("Firmaya özel serbest tanım/etiket; rapor ve şablonlarda yer tutucu olarak kullanılır.", None),
    "CompanyTrainingProgressMode": (
        "Firmanın uzaktan eğitim portalındaki ilerleme modu ayarı.",
        "Personelin konuları sırayla mı yoksa serbestçe mi geçebileceğini belirler.",
    ),
    "CompanyStandardDocument": (
        "Firmanın bir standart belge türü için sunduğu ve onaylanan somut belge.",
        None,
    ),
    "OfficeExpense": ("Kurumun/ofisin firma operasyonları için yaptığı gider.", None),
    "Person": (
        "Firma personeli olmayan gerçek kişi: işveren vekili, ziyaretçi, taşeron çalışanı.",
        "T.C. kimlik numarası şifrelidir.",
    ),
    "RouteOrigin": ("Ziyaret rotasının başlangıç noktası.", "Firmalara olan mesafeler buradan ölçülür."),
    "RouteOriginDistance": (
        "Başlangıç noktası ile firma arasındaki karayolu mesafesi.",
        "Önbellek kaydıdır: harita servisinden bir kez hesaplanır, ziyaret planlanırken tekrar "
        "tekrar sorgulanmaz. OriginId polimorfiktir (il ya da ilçe).",
    ),

    # -- Lookups ------------------------------------------------------------------------
    "City": ("İl referansı.", "Host (kiracısız) referans tablosu — tüm kiracılar paylaşır."),
    "District": ("İlçe referansı.", "Host referans tablosu."),
    "Neighborhood": ("Mahalle referansı.", "Host referans tablosu, salt okunur."),
    "Duty": ('Görev/unvan referansı ("İş Güvenliği Uzmanı", "İşyeri Hekimi").', None),
    "Certificate": ('Sertifika türü referansı ("A Sınıfı İş Güvenliği Uzmanlığı Belgesi").', None),
    "OccupationCode": (
        "NACE meslek/faaliyet kodu referansı.",
        "İşyerinin tehlike sınıfı bu kayıttan türetilir — yani yükümlülüklerin sıklığını "
        "belirleyen zincirin ilk halkası.",
    ),
    "Period": ('Tekrarlayan iş/muayene periyodu tanımı ("6 ayda bir", "yıllık").', None),
    "Parameter": ("Kiracıya özel anahtar/değer sistem ayarı.", None),
    "SystemSetting": ("Sistem geneli yapılandırma değeri (SMTP ayarı, API anahtarı).", None),
    "NumberSequence": (
        "Belge numarası sayacı (teklif no, sözleşme no).",
        "ScopeId polimorfiktir: serinin sahibi Type sütununa göre firma, ofis veya kurum olabilir.",
    ),
    "Tree": ("Hiyerarşik kod listesinin kökü (mevzuat madde ağacı, tehlike sınıflandırması).", None),
    "TreeNode": ("Hiyerarşik kod listesinin bir maddesi.", None),
    "MessageTemplate": ("Uygulama içi bildirim şablonu.", None),
    "MessageTemplateType": ("Bildirim kategorisi: başarı, hata, uyarı.", None),
    "Log": (
        "Sistem/uygulama log kaydı.",
        "Eski sistemdeki Log_T 7,7 milyon satırdı ve göç kapsamına ALINMADI: operasyonel iz "
        "kaydıdır, yeni sistemde yeniden birikir.",
    ),

    # -- Risks --------------------------------------------------------------------------
    "RiskAssessmentReport": (
        "Risk değerlendirme raporunun BAŞLIK kaydı.",
        "Eski RiskAnalizRaporu_T devasa ve düz bir tabloydu: onlarca boolean sütun, tekrarlı "
        "metin alanları. Altı ayrı tabloya bölündü (aşağıdaki RiskAssessment* kayıtları).",
    ),
    "IdentifiedHazard": (
        "Raporda belirlenen tek bir tehlike satırı.",
        "1.000.330 satırla sistemin en kalabalık ikinci tablosu. Göç sırasında Measure alanının "
        "13.004 satırda 2.000 karakteri aştığı görüldü ve sütun sınırsıza çıkarıldı.",
    ),
    "ControlMeasure": ("Belirlenen bir tehlike için alınan/alınacak tek bir önlem.", None),
    "Hazard": (
        "Tehlike kütüphanesi kaydı.",
        "Risk raporları hazır tehlike/risk/önlem üçlüsünü buradan seçer; her rapor sıfırdan "
        "yazılmaz.",
    ),
    "HazardCategory": (
        "Tehlike kütüphanesindeki kategori düğümü.",
        "KİRACI KARARI: eski tabloda kiracı sütunu yoktu; kütüphane kiracıya özelleştirilebilsin "
        "diye TenantId eklendi.",
    ),
    "RiskAssessmentControlMeasure": (
        'Raporda seçilen "mevcut kontrol önlemleri".',
        "NORMALİZASYON: eski tablodaki yedi ayrı MKO* boolean sütununun yerine geçti. Yeni bir "
        "önlem türü eklemek artık şema değişikliği gerektirmiyor.",
    ),
    "RiskAssessmentExposedGroup": (
        'Raporda seçilen "tehlikeden etkilenen gruplar".',
        "NORMALİZASYON: eski tablodaki on ayrı TMK* boolean sütununun yerine geçti.",
    ),
    "RiskAssessmentImprovementAction": (
        'Raporda seçilen "iyileştirme faaliyetleri".',
        "NORMALİZASYON: eski tablodaki yedi ayrı IO* boolean sütununun yerine geçti.",
    ),
    "RiskAssessmentProtectedGroup": (
        "İşyerinde bulunan, özel politika gerektiren çalışan grupları.",
        "NORMALİZASYON: eski Kadın/Genç/Engelli gibi ayrı boolean sütunların yerine geçti.",
    ),
    "RiskAssessmentParticipant": (
        "Risk değerlendirme ekibinin bir üyesi.",
        "NORMALİZASYON: eskiden başlık tablosunda çalışan temsilcisi, destek personeli gibi "
        "ayrı ayrı metin sütunlarıydı.",
    ),
    "RiskAssessmentHistoryRecord": (
        'Raporun "geçmiş" bölümü: iş kazaları, ramak kalalar, meslek hastalıkları.',
        "NORMALİZASYON: başlıktaki düz sütun grubundan satırlara taşındı.",
    ),
    "CorrectiveAction": (
        "Düzeltici ve önleyici faaliyet kaydı (DÖF).",
        "Eski OperationResult sütunu 0/1/-1 tamsayısıydı; anlamı okunabilir bir enum'a çevrildi.",
    ),
    "FieldObservationReport": ("Saha gözlem — işyeri denetim turu — raporunun başlığı.", None),
    "FieldObservationLine": ("Saha gözlem raporundaki tek bir uygunsuzluk satırı.", None),
    "Incident": (
        "Olay kaydı: iş kazası, ramak kala veya meslek hastalığı.",
        "Olay türü eskiden tamsayıydı, enum'a çevrildi.",
    ),
    "IncidentPerson": (
        "Olaya karışan kişi: etkilenen, tanık veya müdahale eden.",
        "Eski OlayKisi_T tablosu tutarsız tanımlanmıştı; kişi rolü enum'a çevrildi.",
    ),
    "Equipment": (
        "Periyodik kontrole tabi iş ekipmanı: makine, tesisat, kaldırma-iletme aracı, basınçlı kap.",
        None,
    ),
    "EquipmentDocument": ("Ekipmana iliştirilen belge veya muayene sertifikası.", None),
    "EquipmentDocumentType": ("Kuruma özel ekipman belge türü listesi.", None),
    "EmergencyActionPlan": (
        "Acil durum eylem planının başlığı.",
        "NORMALİZASYON: eski tablo planın tüm serbest metin bölümlerini tek satırda taşıyordu.",
    ),
    "EmergencyPlanSection": (
        "Acil durum eylem planının tek bir serbest metin bölümü.",
        "NORMALİZASYON: eski tablodaki dokuz düz metin sütununun yerine geçti.",
    ),
    "EmergencyTeamMember": ("Acil durum eylem planında görevlendirilmiş ekip üyesi.", None),

    # -- Plans --------------------------------------------------------------------------
    "WorkPlan": (
        "Firmanın yıllık İSG çalışma planının başlığı — kapak sayfası.",
        None,
    ),
    "WorkPlanLine": (
        "Çalışma planının tek bir satırı: belirli bir ayda yapılacak tek bir faaliyet.",
        "1.045.151 satır. Göçte kritik bir hata yakalandı: Durum = -1 değeri baştan geçersiz "
        "sayılıp atılmıştı; oysa eski uygulama satıra bağlı belge silindiğinde bunu yazıyor ve "
        "satır 'yapılmadı'ya dönüyor. 159.405 satırın durumu bu yüzden yeniden okundu.",
    ),
    "Activity": (
        "Çalışma planına eklenebilecek faaliyet, doküman veya revizyon tanımı (katalog).",
        "RelationId polimorfiktir: hangi tabloya baktığı RelatedTable sütunundaki tablo adına "
        "göre değişir.",
    ),
    "ActivityGroup": ("Faaliyet kategorisi.", None),
    "ActivityDuty": (
        "Faaliyeti, sorumlusu olan görev/unvana bağlayan tablo.",
        "Eskiden yalnızca serbest metin görev kodu tutuluyordu; gerçek bir bağlantıya çevrildi.",
    ),
    "ActivityPeriod": ("Faaliyeti periyot tanımına bağlayan tablo.", None),
    "ContractTemplate": (
        "Sözleşme aşamasında kullanılan çalışma planı şablonu.",
        "DİKKAT: eski sistemde adı ayarlar tablosuydu ama içeriği şablondu.",
    ),

    # -- Trainings ----------------------------------------------------------------------
    "Training": ("Eğitim tanımı (katalog) — uzaktan eğitim platformunda personele atanan şablon.", None),
    "TrainingGroup": ('Eğitim kategorisi: "Genel Konular", "Teknik Eğitimler".', None),
    "TrainingTopic": ("Bir eğitimin tek bir konusu — bir uzaktan eğitim sunumu.", None),
    "TrainingPlan": ("Firmanın yıllık eğitim planının başlığı.", None),
    "TrainingPlanLine": (
        "Eğitim planının tek bir satırı: belirli bir ayda yapılacak tek bir eğitim.",
        "894.571 satır. Göçte 70.774 satırın var olmayan bir plana işaret ettiği görüldü — "
        "kaynaktaki bozuk referans bütünlüğü; atlandı ve raporlandı.",
    ),
    "TrainingDuration": (
        "Bir eğitimin işyeri tehlike sınıfına göre zorunlu süresi (dakika).",
        "NORMALİZASYON: eskiden az/tehlikeli/çok tehlikeli diye üç düz sütundu.",
    ),
    "TrainingTopicDuration": (
        "Bir eğitim konusunun tehlike sınıfına göre süresi (dakika).",
        "NORMALİZASYON: eskiden üç düz sütundu.",
    ),
    "Exam": ("Sınav başlığı.", None),
    "ExamQuestion": ("Sınava ait tek bir soru.", "Eski doğru cevap sütunu şıklara bağlandı."),
    "ExamAnswer": (
        "Sınav sorusunun tek bir şıkkı.",
        "NORMALİZASYON: eskiden şıklar sorunun içinde düz sütunlardı.",
    ),
    "TrainingExam": (
        "Eğitimi sınava bağlayan tablo.",
        "Eski adı TopicTest_T idi ama bağlantısı konuya değil eğitime gidiyordu; isim düzeltildi.",
    ),
    "EmployeeTrainingProgress": ("Personelin bir eğitimdeki uzaktan eğitim ilerlemesi.", None),
    "EmployeeExamAnswer": ("Personelin bir sınav sorusuna verdiği cevap.", None),
    "EmployeeTrainingLog": (
        "Personelin uzaktan eğitim portalındaki her hareketinin iz kaydı.",
        "Eski karşılığı 19,8 milyon satırdı ve göç kapsamına ALINMADI: operasyonel iz kaydıdır, "
        "yeni sistemde yeniden birikir.",
    ),

    # -- Health -------------------------------------------------------------------------
    "MedicalExaminationForm": (
        "Sağlık gözetimi muayene formunun BAŞLIK ve SONUÇ kaydı (EK-2 formu).",
        "Eski form tek satırda yüzlerce sütun taşıyordu; altı ayrı satır tablosuna bölündü. "
        "Yaklaşık 120 şifreli sütunu vardır, bu yüzden toplu kopyalama ile değil DbContext "
        "üzerinden yazılmak zorundadır.",
    ),
    "MedicalExamComplaint": (
        "Muayene formunun anamnez/şikâyet satırı.",
        "NORMALİZASYON: eski formdaki ~23 ayrı şifreli sütunun yerine geçti.",
    ),
    "MedicalExamPhysicalFinding": (
        "Fizik muayene bulgusu satırı (her vücut sistemi için bir satır).",
        "NORMALİZASYON: eski formdaki sistem sistem ayrı sütunların yerine geçti.",
    ),
    "MedicalExamLabTest": (
        "Laboratuvar/tetkik satırı.",
        "NORMALİZASYON: eskiden iki ayrı sütun grubuydu, tek tabloda birleştirildi.",
    ),
    "MedicalExamHabit": (
        "Alışkanlık satırı (tütün, alkol, madde).",
        "NORMALİZASYON: eski iki sütunun yerine geçti; artık miktar ve süre ayrı ayrı tutulur.",
    ),
    "MedicalExamImmunization": ("Muayenede beyan edilen aşı satırı.", "NORMALİZASYON."),
    "MedicalExamWorkCondition": (
        '"Bu koşulda çalışmaya uygun mu?" değerlendirme satırı.',
        "NORMALİZASYON: eski şifreli düz sütunların yerine geçti.",
    ),
    "EPrescription": (
        "İşyeri hekiminin yazdığı e-reçetenin başlığı (MEDULA kaydı).",
        "334.637 reçetenin 321.973'ü (%96) T.C. kimlik numarası üzerinden kendi kiracısındaki "
        "personel kaydına bağlanabildi; kalanı eski veride eşleşmiyor.",
    ),
    "EPrescriptionMedication": ("E-reçetenin ilaç satırı.", "SKRS referansları yalnızca FK ile bağlanır."),
    "EPrescriptionDiagnosis": ("E-reçetenin tanı (ICD-10) satırı.", None),
    "Medication": ("SKRS ilaç referansı — e-reçete ilaç listesi.", "Host referans tablosu, 18.663 kayıt."),
    "Icd10": ("SKRS ICD-10 tanı kodu referansı.", "Host referans tablosu, 15.774 kayıt."),
    "MedicationDoseUnit": ("SKRS ilaç doz birimi kod listesi (tablet, ölçek, ml).", None),
    "MedicationFrequencyUnit": ("SKRS kullanım periyodu birimi kod listesi (saat, gün, hafta).", None),
    "MedicationRoute": ("SKRS ilaç uygulama yolu kod listesi (ağızdan, kas içi, damar içi).", None),

    # -- Documents ----------------------------------------------------------------------
    "Document": (
        "Merkezî belge deposu — sistemdeki HER ikili içeriğin tek kaydı.",
        "Eski sistemde belgeler modül modül farklı tablolara dağılmıştı. Tek tabloda "
        "toplandı: yetki kontrolü, saklama ve SHA-256 doğrulaması tek yerden yapılır. "
        "OwnerRecordId polimorfiktir — sahibi OwnerType'a göre değişir.",
    ),
    "DocumentCategory": ('Belge kategorisi referansı: "Risk Değerlendirme Raporu", "Sağlık Raporu".', None),
    "Form": ('İndirilebilir örnek form/şablon: "İşe Giriş Muayene Formu".', None),
    "FormCategory": ('Form kategorisi: "Risk Değerlendirme Formları".', None),
    "StandardDocument": ("Firmalardan periyodik olarak istenen standart belge türü tanımı.", None),
    "Archive": (
        "Modül bazlı arşiv kaydı: bir faaliyetin ürettiği belgenin hangi modül, satır ve aya ait olduğu.",
        "LineId polimorfiktir — hangi satır tablosuna baktığı arşivlenen modüle göre değişir.",
    ),
    "ModuleArchive": ('Ofis bazlı toplu modül arşivinin başlığı: "Ocak 2026 bordro paketi".', None),
    "ModuleArchiveItem": ("Başlık altındaki tek bir ofis belgesi kaydı.", None),
    "NewsletterSubscriber": ("Bülten e-posta abonesi.", None),

    # -- Finance ------------------------------------------------------------------------
    "Invoice": ("Satış veya alış faturasının başlığı.", "Tutarlar her zaman decimal'dir."),
    "InvoiceLine": ("Fatura satırı.", "Faturanın KDV kırılımı ve toplamları bu satırlardan hesaplanır."),
    "InvoiceTemplate": ("Fatura tasarım şablonu; bir modülün baskı düzenlerini tutar.", None),
    "ServiceItem": ("Faturalanabilir hizmet kalemi — fiyat listesi girdisi.", None),
    "PaymentMethod": ("Ödeme/tahsilat yöntemi: nakit, kredi kartı, havale.", None),
    "Payment": ("Müşterinin bildirdiği banka ödemesi — onay bekleyen tahsilat bildirimi.", None),
    "Bank": ("Kurumun tahsilat için kullandığı banka hesabı.", None),
    "CashRegister": ("Ofis veya kurum kasası.", None),
    "CashTransaction": (
        "Kasa hareketi (giriş/çıkış).",
        "SourceRecordId polimorfiktir: kaynak modüle göre farklı tabloya bakar.",
    ),
    "ExpenseCategory": ("Kasa çıkışlarında kullanılan hiyerarşik gider kategorisi.", None),
    "Penalty": ("6331 sayılı Kanun kapsamında uygulanabilecek idari para cezası, madde bazında.", None),
    "PenaltyAmount": (
        "Bir ceza maddesinin tehlike sınıfı ve çalışan sayısı aralığına göre tutarı.",
        "YENİ VARLIK. Eskiden tutarlar tek satırda düz sütunlardı; her yıl değişen tutarları "
        "yönetmek için satırlara ayrıldı.",
    ),
    "PenaltySurvey": ('Aday müşteri için doldurulan "ceza riski" anketinin başlığı.', None),
    "PenaltySurveyLine": ("Ankette tek bir maddeye verilen cevap.", None),

    # -- Ibys ---------------------------------------------------------------------------
    "IbysQuery": (
        "IBYS'ye (İSG Bilgi Yönetim Sistemi) gönderilen bildirim/sorgu kaydı.",
        "Eski gönderim durum kodu TAŞINMADI: kodu üreten servis o günden beri değişti ve "
        "doğrulanamıyor. Kabul edilmiş gibi göstermektense gönderilmemişten başlamak doğru; "
        "orijinal kod ve mesaj metin olarak saklandı.",
    ),
    "IbysServedWorkplace": ("OSGB'nin IBYS'ye bildirdiği hizmet verilen işyeri kaydı.", None),
    "IbysRootReferenceValue": ("IBYS kök referans değeri — alt değerlerin gruplama düğümü.", None),
    "IbysChildReferenceValue": ("IBYS bağlı (alt) referans değeri.", None),
    "IbysWorkEquipment": ("IBYS iş ekipmanı kodu.", "Muayene formundaki ekipman kodları buradan gelir."),
    "IbysEquipmentTopCategory": ("IBYS iş ekipmanı üst kategorisi.", "Host referans tablosu."),
    "IbysWorkEnvironment": ("IBYS çalışma ortamı kodu.", None),
    "IbysWorkEnvironmentType": ("IBYS çalışma ortamı tipi (üst gruplama).", "Host referans tablosu."),
    "IbysWorkArrangement": ("IBYS çalışma şekli kodu.", None),
    "IbysIsco08OccupationCode": ("ISCO-08 meslek kodu.", "IBYS bildiriminde personelin mesleği buradan seçilir."),
    "ESignatureLicense": ("IBYS bildirimlerini imzalayan e-imza bileşeninin lisansı.", None),

    # -- Communication ------------------------------------------------------------------
    "Visit": (
        "Bir kullanıcının (İSG uzmanı veya hekim) firmaya yaptığı/planladığı ziyaret ve takvim kaydı.",
        "1.733.770 satırla sistemin en kalabalık tablosu. NOT: yapılandırması Communication "
        "klasöründe duruyor; büyük olasılıkla yanlış dosyalanmış, diyagram depoya sadık kalıyor.",
    ),
    "Mail": (
        "Sistemden gönderilen ya da gönderilmeyi bekleyen e-posta.",
        "NORMALİZASYON: ekler eskiden virgüllü metin sütunuydu, ayrı tabloya çıkarıldı.",
    ),
    "MailAttachment": (
        "Bir e-postaya iliştirilmiş belge.",
        "YENİ VARLIK — eski Mail_T.BagliDocuments virgüllü metninin normalize hâli.",
    ),
    "EmailSettings": ("Kurumun posta göndermek için kullandığı POP3/SMTP hesap ayarları.", None),
    "Message": (
        "Kullanıcılar/personel arasındaki uygulama içi mesaj.",
        "SenderId ve RecipientId polimorfiktir: gönderen bir kullanıcı da personel de olabilir, "
        "bu yüzden diyagramda ok çizilmez.",
    ),
    "SupportTicket": ("Kullanıcının açtığı destek talebinin başlığı.", None),
    "SupportTicketMessage": ("Destek talebindeki tek bir mesaj.", None),

    # -- Reports ------------------------------------------------------------------------
    "ActivityReport": ("Firma için üretilen dönemsel faaliyet raporunun başlığı.", None),
    "ActivityReportLine": ("Faaliyet raporu içindeki tek bir veri satırı.", None),
    "OhsReport": (
        "Bir İSG uzmanı veya işyeri hekiminin dönem içindeki hizmet saatleri ve atamalarının özeti.",
        None,
    ),
    "OhsReportHazardClassBreakdown": (
        "Raporun kapsadığı firma sayısının tehlike sınıfına göre dağılımı.",
        "YENİ VARLIK — eskiden düz sütunlardı.",
    ),
    "YearEndReviewReport": (
        "Yıl sonu değerlendirme raporunun başlığı — İSG kurulunun yıllık değerlendirmesi.",
        None,
    ),
    "YearEndReviewLine": ("Yıl sonu değerlendirme raporundaki tek bir faaliyet satırı.", None),
    "SnapshotReport": (
        "Dönemsel anlık görüntü tablosu.",
        "Ağır toplama ve istatistik ekranları milyonlarca satırı taramak yerine önceden "
        "hesaplanmış JSON içeriği okusun diye var.",
    ),

    # -- Infrastructure -----------------------------------------------------------------
    "__EnsaMigrationsHistory": (
        "Entity Framework'ün uygulanmış migration kayıtları.",
        "Uygulamanın değil, EF'in kendi defteri. Şemanın hangi sürümde olduğunu buradan bilir.",
    ),
}

MODULE_NOTES: dict[str, str] = {
    "Tenancy": "Sistemi işleten kurumlar, ofisleri ve sözleşmeleri. Çok kiracılılığın kökü burası.",
    "Membership": "Kullanıcılar, roller ve izin kataloğu. Bir yetkinin çalışması için kurum türü, "
                  "abonelik paketi ve kullanıcı kapılarının üçünden de geçmesi gerekir.",
    "Identity": "OpenIddict 7'nin token, yetkilendirme ve istemci deposu.",
    "Menus": "Gezinme ağacı ve kiracıya/kullanıcıya özel menü değişiklikleri.",
    "Companies": "Müşteri firmalar, bölümleri, personeli ve ekipmanı. Alanın çekirdeği.",
    "Lookups": "İl, ilçe, faaliyet, tehlike sınıfı gibi referans veriler. Çoğu host seviyesinde "
               "tutulur, yani tüm kiracılar aynı listeyi paylaşır.",
    "Risks": "Risk değerlendirmeleri, belirlenen tehlikeler, DÖF'ler ve olaylar. Eski sistemin "
             "devasa düz tabloları burada normalize edildi.",
    "Plans": "Yıllık çalışma planları ve üzerlerinde işaretlenen satırlar.",
    "Trainings": "Eğitim kataloğu, planlar, uzaktan eğitim ilerlemesi ve sınavlar.",
    "Health": "Sağlık gözetimi muayeneleri, e-reçeteler, ilaç ve tanı kod listeleri. Şifreli "
              "sütunların büyük kısmı bu modülde.",
    "Documents": "Saklanan dosyalar ve bir belgeyi bir kayda bağlayan her şey. Tüm ikili içerik "
                 "tek bir Document tablosunda toplandı.",
    "Finance": "Faturalar, kasalar, cari hareketler ve cezalar. Para her zaman decimal.",
    "Ibys": "Bakanlığın İSG Bilgi Yönetim Sistemi'ne yapılan bildirimler ve kod listeleri.",
    "Communication": "Ziyaretler, giden posta, bildirimler ve destek talepleri.",
    "Reports": "Üretilen rapor tanımları ve kaydedilmiş çıktıları.",
    "Infrastructure": "Entity Framework'ün kendi defteri.",
}
