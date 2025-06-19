
# VR-Museum (0416Ver-Unity)
Cilab與曹松清畫家合作的手機端的VR美術館，可上架到ios、android系統的手機上，館內的畫作由畫家繪製，本專案利用Pika、Sora等工具將畫作製作成動畫，並置於自製的VR虛擬環境中讓觀眾觀賞

# 專案介紹
本專案分成三大部分：Unity開發app、影片生成、影片聲音
## 交接
希望到時候一個人生成影片一個人生成聲音，然後兩個人都要學Unity
其餘小部分(最好都要知道)：Apple app上架、cardboard VR眼鏡、畫家舊專案(剪刀石頭布)

# Unity開發app(即此git hub專案)
## 安裝
先fork此專案後clone，之後就幾乎都在fork上開發，等到需要上傳的時候才發pull request

## 使用的插件
### Museum Interior (付費，可登入實驗室帳號後下載)
裡面有很多美術館用的到的材質跟物件
### Post Processing Profiles (付費，可登入實驗室帳號後下載)
裡面有很多的Post Processing效果，簡單來說就是濾鏡
### Google Cardboard XR (免費)
目前此專案只用了他提供的Player物件並參考相關腳本

## 新增畫作：
1. Unity中如果要新增畫作，複製一個畫作物件，命名為[畫作名]，再複製隨便一個Video Player物件命名為Video Player[畫作簡稱]<Br>
2. 將影片放到video資料夾<Br>
3. 在Assets中create一個Render texture物件(VideoOutput物件)命名為VideoOutput[畫作簡稱]大小設為跟畫作等比例(比原本大即可)<Br>
4. 將他的VideoOutput物件及畫作品影片放到Video Player物件中<Br>
5. 將Video Player物件放到畫作物件的video中<Br>
6. VideoOutput也要放入video的Raw Image中<Br>
7. 最後調整畫作物件的video及其碰撞箱大小，以及frame的大小<Br>
8. 將畫作的Discription放到合適的位置並放上對應的文字<Br>
9. 將整個畫作物件放到你要的位置<Br>

## 關於遊戲場景
1. 雖然遊戲有兩個模式(手機、VR)但本專案的遊戲主場景只有一個，即Game Scene，會根據Menu場景的選擇來挑選遊戲格式，啟用對應按鈕及遊戲運作模式<Br>
2. 如果unity中測試畫面(渲染)跟手機畫面不太一樣很正常<Br>
3. 每個場景都可能會有2D部分跟3D部分，2D部分就是放在Canvas物件中的元素會直接顯示在使用者遊玩的畫面上，3D部分則是使用者看到的場景<Br>

## 未來進度(元智大展前必須完成)
1. 持續新增畫作與擴建美術館(九月至少要擴增到總數20幅，為元智大展做準備)
2. 元智大展將不會有VＲ模式（cardboardVR的使用者體驗不夠好，決定保留手機模式並投影到大銀幕），考慮將其直接拔除或偷偷保留在遊戲內部（未來再一起討論）
3. 為所有畫作增加聲音功能，為描述頁增加聲音按鈕(請畫家配音或是收集畫家的聲音做文字轉AI模仿語音)
4. 目前安卓部分僅上架到github page，這很不方便使用者更新，也不適合未來維護，未來安卓部分請上架到Play Store

# 影片生成：
使用Pika labs： https://hackmd.io/@2e8MJipGRW2qQ0gzEQbgWA/HJQh-VHXlx
或使用framePackStoryboard(開發中，未來比賽用)

# MMAudio(聲音生成模型）

MMAudio 是一個音訊生成系統，支援 CLI 和 Gradio 網頁操作。以下為本地端安裝與使用說明。

---

## 安裝需求

- Python 3.9+
- PyTorch 2.5.1+（需搭配正確版本的 `torchvision` 與 `torchaudio`，依照你機器的 CUDA 驅動版本選擇）

使用 CUDA 11.8 的 PyTorch 安裝指令如下：

```bash
pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu118 --upgrade
```

## 安裝步驟
1. 下載原始碼
   ```bash
   git clone https://github.com/hkchengrex/MMAudio.git
   cd MMAudio
   ```
2. 安裝本地套件
   ```bash
   pip install -e .
   ```
   若出現 `setup.py not found` 錯誤，請先升級 pip：
   
   ```bash
   pip install --upgrade pip
   ```

## 執行
1. 在 MMAudio 資料夾中新建一個資料夾 input

2. 把要生成聲音模型的影片放到 input 資料夾當中

3. 命令列模式
   ```bash
   python demo.py --duration 8 --video "input\你的影片名字.mp4" --prompt "裡面是你要打的東西（用英文）" --negative_prompt "裡面是你要打的東西（用英文）"
   ```
   duration 後面的8代表秒數（看你影片多長就改成幾秒）

4. 跑跑跑！等待一下（應該不超過5分鐘）

5. 產生的音訊（.flac）與影片（.mp4）會輸出至 output 資料夾。



