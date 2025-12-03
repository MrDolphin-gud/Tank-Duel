# Tank Duel

Unity ile geliştirilmiş fizik tabanlı mekaniklere sahip bir Tank Savaşı oyunu.

## Oyun

Oyunu tarayıcı üzerinden oynamak için tıklayın:

https://mrdolph1n.itch.io/tank-duel?secret=op79GSR9ibEX3M87286qDZpFdI

## Proje Hakkında

Bu proje, Unity Oyun Motoru kullanılarak geliştirilmiştir. Oyuncu, bir tankı kontrol ederek çeşitli yetenekler ve fizik tabanlı etkileşimler kullanarak hayatta kalmaya çalışır.

## Mekanikler
Oyuncu tankı ile Düşman tankı aynı özelliklere sahip ancak birbirini etkilemiyor.
### Ateş Etme
Fizik tabanlı mermi fırlatma sistemi.

**Özellik:** Her atışta tankta fiziksel ve görsel geri tepme oluşur.

### Dash / Atılma
Fizik motoruna (AddForce) anlık güç uygulanarak yapılan ani manevra.

**Kullanım:** Düşman ateşinden kaçmak için kullanılır.

### Mayın Bırakma
Tankın arkasına bırakılan ve tetikleyici (Trigger) ile çalışan patlayıcı.

**Mekanik:** Alan kontrolü sağlar, üzerine basıldığında yok olur ve hasar verir.

### Enerji Kalkanı
Belirli bir süre boyunca gelen hasarı 0'a indiren savunma sistemi.

**Görsel:** Kalkan aktifken tankın rengi değişir ve hasar alınmaz.

## Kontroller

| Tuş | Eylem | Açıklama |
|-----|-------|----------|
| **W / S** | Hareket | İleri ve Geri sürüş. |
| **A / D** | Gövde Dönüşü | Tank gövdesini sağa veya sola döndürür. |
| **Q / E** | Taret Dönüşü | Taret ve namluyu gövdeden bağımsız döndürür. |
| **SPACE** | Ateş | Mermi fırlatır. |
| **SHIFT** | Dash | Hızlı ileri atılma. |
| **F** | Mayın | Arkaya mayın bırakır. |
| **R** | Kalkan | 3 saniye hasar bağışıklığı sağlar. |
| **ESC** | Durdur | Pause Menüsünü açar ve kapatır. |

## Ses Sistemi ve Atmosfer

Oyun atmosferini güçlendirmek için 3D Spatial Audio (Surround) kullanılmıştır:

- **Motor Sesi:** Tank hızlandıkça motorun sesi artar. Durunca sessizleşir.
- **Taret Sesi:** Sadece taret dönerken mekanik ses çalar.
- **Patlamalar:** Mayın patlamaları PlayClipAtPoint kullanılarak patlamanın olduğu konumdan duyulur.
- **Ayarlar:** Ana menüden Müzik ve Efekt sesleri ayrı ayrı ayarlanabilir ve kaydedilir.

## Yapay Zeka 

Şu anki sürümde rakip tank temel fizik ve can sistemi bileşenlerine sahiptir. Projenin ilerleyen aşamalarında rakip tankın kontrolü bir Yapay Zeka ajanına evrilecektir.




