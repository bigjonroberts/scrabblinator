# Glossiator
Glossary micro service with the intention to integrate it with Slack's [Slash Commands](https://api.slack.com/slash-commands) API.

[![Build Status](https://travis-ci.org/dustinmoris/Glossiator.svg)](https://travis-ci.org/dustinmoris/Glossiator)

[![Build History](https://buildstats.info/travisci/chart/dustinmoris/Glossiator)](https://travis-ci.org/dustinmoris/Glossiator/builds)

## How it works

Glossiator is a micro service which can be hosted in your own environment by running the following Docker command:

```
docker run -d -p 8083:8083 -e URL_OR_PATH_TO_CSV="{path to csv file}" -e TOKEN="{secret token}" dustinmoris/glossiator:latest
```

The Docker container needs to be launched with two environment variables:

- URL_OR_PATH_TO_CSV
- TOKEN

`URL_OR_PATH_TO_CSV` must be an absolute local path or a web URL to a CSV file which contains all the glossary terms. `TOKEN` is the secret token that you will be given from Slack's [Slash Commands](https://api.slack.com/slash-commands) configuration page (see below).

The CSV file is expected to have three columns:

- Term
- Meaning
- Description

Term is usually the key or an abbreviation that your team would be searching for. Meaning is a short description and Description is a more detailed explanation.

| Example | |
| :--- | :--- |
| Term: | CI |
| Meaning: | Continuous Integration |
| Description: | Continuous Integration (CI) is a development practice that requires developers to integrate code into a shared repository several times a day. Each check-in is then verified by an automated build, allowing teams to detect problems early. |

The next step is to integrate it with Slack's [Slash Commands](https://api.slack.com/slash-commands) API.

1. Click on the "Add Configuration" button to add a new configuration.
2. Enter the desired command. e.g. `/whatis`
3. Enter the URL to the self hosted Glossiator app. The correct endpoint is called `/whatis`.
  - Example: `http://glossiator.my-server.com/whatis`
4. Choose `POST` from the method drop down.
5. Use the auto-generated token from the Token field to configure the Docker container (see above).
6. Configure the remaining fields to your likes. Pick a name and icon for the Slack bot and fill in the optional help text.
7. Click "Save Integration" and the setup is complete!

Now you should be able to go into your Slack channel and be able to type `/whatis some-term` and get a successful response from the Glossiator app.

## License

GNU General Public License v3