# ![Logo](.docs/mentat.png) Mentat

[![Docker](https://img.shields.io/docker/v/gendalf90/mentat)](https://hub.docker.com/r/gendalf90/mentat)

> A process cannot be understood by stopping it. Understanding must move with the flow of the process, must join it and flow with it.

## What is it?

The Mentat is a bot that you can communicate with by email. The bot uses IMAP and SMTP protocols to address mail server and OpenAI compatible api to answer your requests and keeps context in chains of emails.

## How it works?

To run Mentat you can use docker image:

```bash
docker run -d \
  --name mentat \
  -e PollInterval='00:00:05' \ # by default
  -e OpenAIUrl='http://1.2.3.4:1234/' \ # by default http://localhost:11434/ (with --network=host for example)
  -e OpenAIModel='deepseek-r1:14b' \
  -e MailImapHost='imap.gmail.com' \
  -e MailImapPort=993 \ # by default
  -e MailSmtpHost='smtp.gmail.com' \
  -e MailSmtpPort=465 \ # by default
  -e MailUsers='user@mail.com' \ # list of users separated by ',' to response by bot
  -e MailLogin='bot@gmail.com' \ # bot user for login at server
  -e MailPassword='asdf1234' \ # app password for login at server
  --restart=unless-stopped \
  gendalf90/mentat:latest
```

Then just send an email with question from `user@mail.com` to `bot@gmail.com` and wait for response. If you want to use the context of the conversation so just reply the last response from bot.
