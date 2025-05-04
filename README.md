# Neon

A .NET application to help manage your twitch stream. 

Planned features include:
- Management console
- Chatbot with custom commands
- Stream alerts
- Stream chat overlay

The application can be ran locally by using the docker compose files, or navigate to web hosted version at [TBD] url.

## Management Console

A web application that allows you to login using your twitch account and manage your settings. Configurable chat commands, stream alerts, and chat message overlays with customization.

The integration will not be used for managing your actual stream settings, but rather how the bot interacts with your stream.

## Chatbot

Triggers responses to chat messages based off configured commands that you control through the management console. Commands can be toggled on and off at any time.

Additional global commands will be available to all users.

Planned global commands:
- !uptime
- !title
- !game
- !followage
- !setgame
- !settitle

## Stream Alerts

Customize alerts for your stream with overlays triggered by channel events.

Planned supported alerts:

- New Follower
- New Subscriber
- New Cheer
- New Raid

## Chat Overlay

A customizable overlay that can be displayed on your stream. The overlay will display chat messages from your stream with full emote support for the core providers.