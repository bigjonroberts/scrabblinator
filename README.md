# Glossiator
Glossary micro service with the intention to integrate it with Slack's [Slash Commands](https://api.slack.com/slash-commands) API.

# Usage

```
docker run -d -p 8083:8083 -e URL_OR_PATH_TO_CSV="{url or local path to a csv file}" dustinmoris/glossiator:latest
```