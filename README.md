# Scrabblinator
[Scrabblinator micro service](https://hub.docker.com/r/bigjonroberts/scrabblinator) with the intention to integrate it with Slack's [Slash Commands](https://api.slack.com/slash-commands) API.

It was copied from [Glossiator](https://github.com/dustinmoris/glossiator/) for the core Slack parsing and response.

## How it works

### Run the Docker container

[Scrabblinator is a micro service](https://hub.docker.com/r/bigjonroberts/scrabblinator/) which can be hosted in your own environment by running the following Docker command:

```
docker run -d -p 8083:8083 -e PREFIX="scrabble-" -e GENMODE="emoji" -e TOKENS="{secret token}" bigjonroberts/scrablinator:latest
```

The Docker container must be launched with at least one environment variable:

- TOKENS

`TOKENS` is a semi-colon `;` delimited list of secret tokens that you will be given from Slack's [Slash Commands](https://api.slack.com/slash-commands) configuration page (see below).

#### Optional parameters

Currently there is one optional environment variable which can be set inside the Docker container to specify the maximum distance between a search term and an entry in the glossary to count as a match: `PREFIX`.

The default value is set to `""`.

### Integrate with Slack's Slash Commands API

The next step is to integrate it with Slack's [Slash Commands](https://api.slack.com/slash-commands) API.

1. Click on the "Add Configuration" button to add a new configuration.
2. Enter the desired command. e.g. `/scrabble`
3. Enter the URL to the self hosted scrabblinator app. The correct endpoint is called `/scrabble`.
  - Example: `https://scrabblinator.my-server.com/scrabble`
4. Choose `POST` from the method drop down.
5. Use the auto-generated token from the Token field to configure the Docker container (see above).
6. Configure the remaining fields to your likes. Pick a name and icon for the Slack bot and fill in the optional help text.
7. Click "Save Integration" and the setup is complete!

Now you should be able to go into your Slack channel and be able to type `/scrabble hello` and get a successful response from the Scrabblinator app.

## License

[GNU General Public License v3](https://raw.githubusercontent.com/dustinmoris/Glossiator/master/LICENSE)
