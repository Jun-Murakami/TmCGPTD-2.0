# TmCGPT Debugger
ChatGPT API Client Application.  
It supports Windows 10 and later, as well as MacOS.  

ChatGPT APIのクライアントアプリです。Windows10以降、MacOSに対応。
  
## Features:  
  
- **Vertical 5-split text editor**  
Designed to make it easy to cut and paste text to configure prompts.
- **Importing web service version logs**  
You can log in with your ChatGPT account and import logs from the web service version. Even if the chat log has reached its maximum length, you can continue the conversation semi-permanently after importing. (It has a feature that automatically compresses and retains old conversation history in the background.) Also, if you are a PLUS member, you can use the fast GPT3.5 model in the built-in browser as is.
- **Preset phrase function**  
You can register frequently used phrases, such as "Please translate ~ into English in several patterns," "You are a professional editor," and "Please summarize the following text in 300 characters or less," and insert them into the editor.
- **Prompt template**  
Prompt templates can be saved and loaded.
- **Text editor history**  
Automatically saves up to 200 entries of sent text history for reuse and tweaking of prompts.
- **API option settings**  
All API options for the Chat model can be adjusted via the GUI.

## 機能:  

- **縦5分割のテキストエディタ**  
文章を切り貼りして命令文／プロンプトを構成しやすいようになっています。
- **Webサービス版ログのインポート**  
ChatGPTのアカウントでログインしてWebサービス版のログをインポートできます。チャットの長さが最大に達しているログでも、インポートした後は半永久的に会話が継続できます。（古い会話履歴を自動的に圧縮し、裏で保持する機能が備わっています。）また、内蔵のブラウザでそのままWeb版のチャットも使用できます。取り込んだ全てのログに対してテキスト全文検索が出来ます。
- **定型文プリセット機能**  
「～を英語で何パターンか翻訳してください。」とか、「あなたはプロの編集者です」「下記の文章を300文字以内で要約してください。」など、よく使う定型文を登録しておいて、エディターに挿入できます。
- **プロンプトテンプレート**  
プロンプトのテンプレートを保存、読み込みできます。
- **テキストエディタ履歴**  
プロンプトを使いまわしたり、微調整したりするために、送信した文章の履歴を200件まで自動的に保存します。
- **APIオプション設定**  
ChatモデルのAPIオプションをGUIで調整できます。

<img width="1144" alt="スクリーンショット 2023-05-27 10 32 38" src="https://github.com/Jun-Murakami/TmCGPTD-2.0/assets/126404131/01f09bbd-8252-4416-946f-a7d997843ad9">
<img width="1260" src="https://user-images.githubusercontent.com/126404131/236693431-4da2e7bc-f9da-4048-829a-9f21d290a335.png">
<img width="1260" alt="スクリーンショット 2023-05-07 04 33 07" src="https://user-images.githubusercontent.com/126404131/236644742-c991c12d-50af-47d2-ab03-66646700c927.png">

(ToDo:Support Google Drive to synchronize settings and logs in the cloud.)

I only have an old Mac, so I haven't been able to test it on newer Macs, like the Silicon Mac. I would appreciate it if you could let me know if there are any issues.

[AvaloniaUI](https://github.com/AvaloniaUI/Avalonia) is used for multi-platform support.

More information (japanese) : お知らせや詳細な解説などはnoteで書いてこうと思います。

https://note.com/junmurakami

by Jun Murakami
