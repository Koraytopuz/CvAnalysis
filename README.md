# Yapay Zeka Destekli CV Analiz Motoru

Bu proje, kullanıcıların CV'lerini ve iş ilanı metinlerini yükleyerek, Azure OpenAI ve Azure Form Recognizer servisleriyle analiz eden, modern ve kullanıcı dostu bir web uygulamasıdır. Amaç, CV'nin ATS (Aday Takip Sistemi) uyumluluğunu ve içerik kalitesini artırmaya yönelik kişiselleştirilmiş öneriler sunmaktır.

## Özellikler

- **CV ve İş İlanı Analizi:** Kullanıcıdan alınan CV ve iş ilanı metni, Azure Form Recognizer ile okunur ve anahtar kelime analizi yapılır.
- **Dinamik Öneriler:** Azure OpenAI (GPT) ile, CV ve iş ilanına özel, Türkçe veya İngilizce, maddeler halinde öneriler üretilir.
- **ATS Puanı ve İpuçları:** CV'nin ATS uyumluluğu puanlanır ve iyileştirme önerileri sunulur.
- **Eksik Anahtar Kelimeler:** İş ilanında olup CV'de olmayan anahtar kelimeler tespit edilir.
- **Modern ve Responsive Arayüz:** Mobil ve masaüstü uyumlu, sade ve modern bir kullanıcı arayüzü.
- **Dil Seçici:** Türkçe ve İngilizce arasında kolayca geçiş yapılabilir.
- **Güvenli Anahtar Yönetimi:** Azure anahtarları ve endpoint bilgileri hiçbir zaman frontend'e veya herkese açık dosyalara eklenmez.

## Kullanılan Teknolojiler

- **Backend:** ASP.NET Core, Azure.AI.FormRecognizer, Azure.AI.OpenAI, Entity Framework Core, SQLite
- **Frontend:** React, Vite, Material-UI (MUI)
- **Diğer:** Azure Cloud Servisleri



## Katkı ve Lisans

- Proje MIT lisansı ile açık kaynak olarak sunulmuştur.
- Katkıda bulunmak için lütfen bir fork oluşturun ve pull request gönderin.

---

Her türlü soru ve destek için bana ulaşabilirsiniz. 