# TmCGPT Debugger
A ChatGPT client that integrates the web services version of ChatGPT and OpenAI API.
It supports Windows 10 and later, as well as MacOS.  

Webサービス版のChatGPTとOpenAI APIを統合するクライアントアプリです。Windows10以降、MacOSに対応。

<img width="1741" alt="TmCGPTD_v25_01" src="https://github.com/Jun-Murakami/TmCGPTD-2.0/assets/126404131/19443f35-8cfa-49f8-a7a5-06a89f52624a">

## New features in version 2.5.0 !

- A new [web app](https://tmcgptd.web.app/) version has been developed to allow logs from the desktop version to be synchronized in the cloud. You can also view the logs on your smartphone. (To use the cloud function/web app version, user registration or login via Google/Microsoft/GitHub account is required.)
- If you set an API key in the web app version, you can chat in the browser. The features are minimal, but like the desktop version, it compresses old conversation histories automatically, allowing semi-permanent continuation of conversations.
- We may limit the number of registered users because of the cost of using the backend service.
(The logs of the desktop version will not be lost even if the cloud service is terminated, since it is still the same as before for local use.)
If you do not need the cloud function, please use [ver 2.4.x](https://github.com/Jun-Murakami/TmCGPTD-2.0/releases/tag/v2.4.7).

## Version 2.5.0 の新機能

- [Webアプリ版](https://tmcgptd.web.app/)を新規開発し、デスクトップ版のログをクラウドで同期できるようにしました。スマホでもログを閲覧できます。（クラウド機能／Webアプリ版を利用するには、ユーザー登録、またはGoogle/Microsoft/GitHubアカウントでのログインが必要です。）
- Webアプリ版にAPIキーを設定すれば、ブラウザでもチャットが可能です。機能は最低限ですが、デスクトップ版と同じく古い会話履歴を自動的に圧縮して半永久的に会話が継続できる機能は実装しています。
- バックエンドサービスのコストの関係で、登録ユーザー数を制限する場合があります。(ローカルで使う分には今までと変わらないので、クラウドサービスが終了してもデスクトップ版のログは消えません。)
クラウド機能が不要な方は [ver 2.4.x](https://github.com/Jun-Murakami/TmCGPTD-2.0/releases/tag/v2.4.7) をご利用ください。

## Features:  
  
- **Vertical 5-split text editor**  
Designed to make it easy to cut and paste text to configure prompts.
- **Importing web service version logs**  
You can log in with your ChatGPT account and import logs from the web service version. Even if the chat log has reached its maximum length, you can continue semi-permanently using the API after importing.
- **Web version of ChatGPT and Goggle Bard support**  
A web version of ChatGPT can be used with the built-in browser. Texts can be sent directly from the prompt editor, and GoogleBard is also supported.
- **Preset phrase function**  
You can register frequently used phrases and insert them into the editor.
- **Prompt template & log**  
Prompt templates can be saved and loaded. Automatically saves up to 200 entries of sent text history.

## 機能:  

- **縦5分割のプロンプトエディタ**  
文章を切り貼りして、長めの命令文／プロンプトを構成しやすいようになっています。送信時は自動的に結合されます。
- **Webサービス版ログのインポート**  
ChatGPTのアカウントでログインしてWebサービス版のログをインポートできます。チャットの長さが最大に達しているログでも、インポートした後は半永久的に会話が継続できます。（古い会話履歴を自動的に圧縮し、裏で保持する機能が備わっています。）取り込んだ全てのログに対してテキスト全文検索が出来ます。
- **Webサービス版チャットとGoggle Bard対応**  
内蔵のブラウザでそのままWeb版のチャットも使用できます。プロンプトエディタから直接文章を送信可能で、GoogleBardにも対応しています。
- **定型句プリセット機能**  
よく使う定型句を登録しておいて、エディターに挿入できます。
- **プロンプトテンプレート＆ログ**  
プロンプトのテンプレートを保存、読み込みできます。送信した文章の履歴も自動的に保存します。

> 複数のコンピューターでチャットログを同期するには、画面右上のデータベースアイコンをクリックして、データベースファイルの保存場所をクラウドドライブ（Dropboxなど）に設定してください。

<img width="1154" alt="スクリーンショット 2023-07-25 20 08 03" src="https://github.com/Jun-Murakami/TmCGPTD-2.0/assets/126404131/f13dd757-f729-4a14-84b8-825b45b49416">
<img width="1154" alt="スクリーンショット 2023-07-25 20 08 18" src="https://github.com/Jun-Murakami/TmCGPTD-2.0/assets/126404131/58adee84-fc57-4daf-aa27-12d3f8960644">
<img width="1154" alt="SC" src="https://github.com/Jun-Murakami/TmCGPTD-2.0/assets/126404131/1fd537ca-f980-4579-a9e0-130a29753a8a">

Web app repo is [here](https://github.com/Jun-Murakami/TmCGPTD-React).

[AvaloniaUI](https://github.com/AvaloniaUI/Avalonia) is used for multi-platform support.

More information (japanese) : お知らせや詳細な解説などはnoteで書いてこうと思います。

https://note.com/junmurakami

by Jun Murakami
