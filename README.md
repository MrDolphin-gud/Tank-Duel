# Tank Duel

Unity ile geliştirilmiş Q-Learning tabanlı yapay zekâ içeren fizik tabanlı bir tank savaşı oyunu.

## Oyun Hakkında

Tank Duel, oyuncunun Q-Learning algoritması ile eğitilmiş yapay zeka tankına karşı savaştığı bir oyundur. Oyuncu ve AI tankları aynı yeteneklere sahiptir: ateş etme, dash, mayın bırakma ve enerji kalkanı.

**Tarayıcıda Oyna: **
[Oyun Hakkinda](https://mrdolph1n.itch.io/tank-duel?secret=op79GSR9ibEX3M87286qDZpFdI)

## Kontroller
 
| W / S | Hareket | İleri ve geri sürüş |

| A / D | Gövde Dönüşü | Tank gövdesini sağa veya sola döndürür |

| Q / E | Taret Dönüşü | Taret ve namluyu gövdeden bağımsız döndürür |

| SPACE | Ateş | Mermi fırlatır |

| SHIFT | Dash | Hızlı ileri atılma |

| F | Mayın | Arkaya mayın bırakır |

| R | Kalkan | 3 saniye hasar bağımsızlığı sağlar |

| ESC | Durdur | Pause Menüsünü açar/kapatır |

## Mekanikler

### Ateş Etme
Fizik tabanlı mermi fırlatma sistemi. Her atışta tankta fiziksel ve görsel geri tepme oluşur.

### Dash / Atılma
Fizik motoruna (AddForce) anlık güç uygulanarak yapılan ani manevra.

### Mayın Bırakma
Tankın arkasına bırakılan ve tetikleyici (Trigger) ile çalsan patlayıcı.

### Enerji Kalkanı
Belirli bir süre boyunca gelen hasarı sıfıra indiren savunma sistemi.

## Yapay Zekâ Sistemi

### Q-Learning Nedir?

Q-Learning, bir ajanın deneme-yanılma yoluyla optimal davranışları öğrendiği bir pekiştirmeli öğrenme algoritmasıdır.

### Q-Learning Formülü

Q(s,a) = Q(s,a) + alpha * (reward + gamma * max(Q(s',a')) - Q(s,a))

- Q(s,a): Durum s'de eylem a'nın değeri
- alpha: Öğrenme oranı (0.5)
- gamma: İndirgeme faktörü (0.9)
- reward: Alınan ödül veya ceza
- max(Q(s',a')): Sonraki durumun maksimum Q-değeri

### State Encoding (Durum Kodlama)

AI tankı 14 farklı giriş kullanır:

| 0 | Mesafe | Hedefe olan mesafe (0-1) |

| 1 | Hedef Tipi | Oyuncu mu? (0/1) |

| 2-3 | Pozisyon | X ve Z koordinatları (0-1) |

| 4-5 | Gövde Açısı | Oyuncuya göre açı ve yon |

| 6-7 | Taret Açısı | Oyuncuya göre açı ve yon |

| 8 | Can | Mevcut can oranı (0-1) |

| 9-11 | Cooldownlar | Dash, Mayın, Kalkan hazır mi? |

| 12 | Kalkan | Kalkan aktif mi? |

| 13 | Hareket | Hareket hızı (0-1) |

### Kayıtlı Eylemler (Actions)

| 0 | MoveForward | İleri hareket |

| 1 | MoveBackward | Geri hareket |

| 2 | RotateLeft | Gövdeyi sola döndür |

| 3 | RotateRight | Gövdeyi sağa döndür |

| 4 | RotateTurretLeft | Tareti sola döndür |

| 5 | RotateTurretRight | Tareti sağa döndür |

| 6 | Fire | Ateş et |

| 7 | Dash | Atılma yap |

| 8 | PlaceMine | Mayın bırak |

| 9 | ActivateShield | Kalkanı aktifle |

| 10 | DoNothing | Hareketi durdur |

## Dosya Yapısı

### Ana Script Dosyaları

| QLearningBrain.cs | Q-Learning algoritması, Q-Table yönetimi |

| QLearningAgent.cs | AI karar verme, durum oluşturma |

| QLearningSpawnManager.cs | Düşman spawn, oyun hızı |

| EnemyController.cs | Tank hareket ve yetenekler |

| Health.cs | Can sistemi, hasar alma |

| Projectile.cs | Mermi davranışı |

| Mine.cs | Mayın davranışı |

| MainMenuManager.cs | Ana menü, AI yükleme |

| TankController.cs | Oyuncu kontrolu |

| PauseMenu.cs | Durdurma menusu |

### Model Dosyası: RLBrain.json

**Yüklenecek Dosya: ** RLBrain.json

**Konum: **
- Unity Editor: Assets/RLBrain.json
- Build: %APPDATA%\..\LocalLow\Bursa Teknik Üniversitesi\Tank Duel\RLBrain.json

**Dosya İçeriği: **
- inputCount: Durum vektörünün boyutu (14)
- actionCount: Eylem sayısı (11)
- qTableEntries: Durum-Q değerleri eşlemeleri
- learnedStateActions: Öğrenilmiş durum-eylem çiftleri

## AI Model Yükleme

### Ana Menüden Model Yükleme

1. Ana menüde Yapay Zekâ Yükle butonuna tıklayın
2. Dosya gezgini açılır
3. RLBrain.json dosyasını secin
4. Buton cyan renk alır
5. Oyunu başlatın

### Buton Durumları

| Sarı | Kapalı | Dosya gezgini açılır |

| Yeşil | Varsayılan model | Kapanır |

| Cyan | Özel model yüklü | Kapanır model temizlenir |

## Konfigürasyon

### Ögrenme Parametreleri

| learningRate | 0.5 | Ögrenme hızı |

| discount | 0.9 | Gelecek ödüllerin önemi |

| exploration | 0.1 | Rastgele eylem olasılığı |

### Karar Araligi

QLearningAgent.cs dosyasında decisionInterval degişkeni ile ayarlanır. Varsayılan: 0,2 saniye

## Ödül ve Ceza Sistemi

Tüm değerler 0-10 arası ölçeklenmiştir.

### Ödüller

| Oyuncuya isabet | +5 |

| Oyuncuyu öldürme | +10 |

| Ateş etme | +1 |

| Gövde/Taret donuk kalma | +2 |

| Kalkan ile engelleme | +3 |

### Cezalar

| Iskalama | -1 |

| Duvara çarpma | -2 |

## Ses Sistemi

- Motor Sesi: Tank hızına bağlı
- Taret Sesi: Sadece taret dönerken
- Patlamalar: Konum tabanlı 3D ses
- Ayarlar: Ana menüden ayarlanabilir





