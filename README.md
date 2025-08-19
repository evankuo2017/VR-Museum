
# VR-Museum (0416Ver-Unity)
Cilab與曹松清畫家合作的VR端的VR美術館，使用HTC VIVE FOCUS3遊玩，館內的畫作由畫家繪製，本專案利用Pika、Sora等工具將畫作製作成動畫，並置於自製的VR虛擬環境中讓觀眾觀賞

# 專案介紹
本專案為原VR美術館app改寫而成
## 交接
希望到時候先了解完app專案再來了解此專案

# Unity開發app(即此git hub專案)
## 安裝
九月：直接clone此專案，沒學長允許不要push
九月後：先fork此專案後clone，之後就幾乎都在fork上開發，等到需要上傳的時候才發pull request

## 新增畫作：
理論上同app的方式

## 關於遊戲場景
雖然遊戲有兩個模式(手機、VR)，但是手機模式在這邊是測試模式會把Test Player打開原本關掉

## 測試遊戲
請把場景中的TestPlayer設為Paintings物件中腳本的玩家，然後再做測試
測試請從Menu場景開始，然後會可以跟app的手機模式用一樣的方式測試(搖桿移動，到scene手動旋轉player空白鍵觸發畫作)

## 未來進度
布展前須完工，能夠正常玩然後介面盡量好看
清理Dirty code，等候未來畫家或老師發落(新增畫作/加入LLM/手勢互動)

# 影片生成：
使用Pika labs： https://hackmd.io/@2e8MJipGRW2qQ0gzEQbgWA/HJQh-VHXlx <Br>
或使用framePackStoryboard(開發中，未來比賽可用)

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



