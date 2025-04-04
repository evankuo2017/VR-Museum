# VR-Museum
Cilab與曹松清畫家合作的手機端的VR美術館，可上架到ios、android系統的手機上，館內的畫作由畫家繪製，本專案利用Pika、Sora等工具將畫作製作成動畫，並置於自製的VR虛擬環境中讓觀眾觀賞
## 使用插件
### Museum Interior (付費，可登入實驗室帳號後下載)
裡面有很多美術館用的到的材質跟物件
### Post Processing Profiles (付費，可登入實驗室帳號後下載)
裡面有很多的Post Processing效果，簡單來說就是濾鏡
### Google Cardboard XR (免費)
目前此專案只用了他提供的Player物件並參考相關腳本

## Notice
**1.每幅畫的最終影片名跟畫作名一致**<Br>
**2.FlowFrames不要把影片弄到幀數x10不然影片放到app裡面只有ROG手機不會卡，之前實測幀數x2的話舊安卓不會卡**<Br>

## 新增畫作：
1.Unity中如果要新增畫作，複製一個畫作物件，命名為[畫作名]，再複製隨便一個Video Player物件命名為Video Player[畫作簡稱]<Br>
2.將畫作放到image資料夾(Texture Type設為Sprite(2D and UI)、影片放到video資料夾<Br>
3.在Assets中create一個Render texture物件(VideoOutput物件)命名為VideoOutput[畫作簡稱]大小設為跟畫作等比例(比原本大即可)<Br>
5.將他的VideoOutput物件及畫作品影片放到Video Player物件中<Br>
6.將Video Player物件放到畫作物件的video中<Br>
7.VideoOutput也要放入video的Raw Image中<Br>
8.最後放入圖片到畫作的Image，調整畫作物件的video、image及其碰撞箱大小，以及frame的大小<Br>
9.將整個畫作物件放到你要的位置<Br>

## 關於遊戲場景
1.本專案的遊戲場景只有一個，即Game Scene，會根據Menu場景的選擇來挑選遊戲格式，啟用對應按鈕及遊戲運作模式<Br>
2.如果unity中測試畫面(渲染)跟手機畫面不太一樣很正常，建議可以在擴建美術館啟用flyToTest物件並用他的camera來做測試(關掉player物件)<Br>
3.每個場景都可能會有2D部分跟3D部分，2D部分就是放在Canvas物件中的元素會直接顯示在使用者遊玩的畫面上，3D部分則是使用者看到的場景<Br>

## 知識：

在 Unity 中，VideoPlayer 可以將影像輸出到一個 Render Texture
（也就是你在專案裡看到的 “video output” 這個 Render Texture
然後再把這個 Render Texture 指定給 Raw Image 來顯示。整個流程大致如下：
VideoPlayer 播放影片
Unity 內建的 VideoPlayer 元件可以把影片的內容即時解碼，並輸出到各種目標
（Camera、Render Texture、Material 等）。
當你在 VideoPlayer 的 Target Texture（或類似參數）欄位中指定了一個 Render Texture，
表示「把解碼出來的每一幀影片畫面輸出到這個 Render Texture 上」。
Raw Image 讀取 Render Texture
之後在 Raw Image 的 Texture 欄位指定同一個 Render Texture。這樣一來，
Raw Image 就能顯示「VideoPlayer 播放後輸出的畫面」。
由於 Raw Image 是屬於 UI 元素，這個做法能很方便地把影片嵌在 Canvas 內或當成 2D UI 的一部分來顯示。
總結來說，這個 “video output” 的 Render Texture 就是用來「接收並儲存」影片當下每一幀的畫面，然後把它貼到你的 UI（Raw Image）上面。
這樣你就能在場景裡，甚至是在 VR/AR 環境中，去控制並顯示影片內容。

## 未來進度
解決影片太多導致app大小膨脹問題
解決影片太多時視角旋轉不流暢問題
持續新增畫作與擴建美術館
