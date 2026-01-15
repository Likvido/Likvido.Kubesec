# Likvido.Kubesec
Tool to ease the management of Kubernetes secrets

## Requirements
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/) needs to be installed and configured for your cluster

## Installation
Install this tool via dotnet global tools:
```
dotnet tool install --global Likvido.Kubesec
```

https://www.nuget.org/packages/Likvido.Kubesec

## How to pull secrets

This is the command to pull secrets:
```
kubesec pull <secret-name> --namespace <kubectl-namespace> --context <kubectl-context-name> --unwrap-key <key-name> --port-forward --output <output-file> --remove-json-field <field-to-remove>
```

This example will pull the secrets stored with the name `sync-creditors` from a namespace called `likvido-api` in a cluster with the kubectl context name `staging`, and then output them to a file called `secrets.yaml`:
```
kubesec pull sync-creditors --namespace likvido-api --context staging --output secrets.yaml
```

If you do not specify `--context`, then it will use whatever context is currently active in kubectl\
If you do not specify `--namespace`, then it will use `default` namespace\
If you do not specify `--output`, then it will display the data inside the console in a table format\
If you do not specify `--unwrap-key` then it will fetch all keys and output them with both their key name and value\
If you do not specify `--port-forward` then it won't do any port forwarding\
If you do not specify `--remove-json-field` then no fields will be removed (duh!). This option can be specified multiple times, in case you have multiple fields you want to remove, see the example further down this readme.

### Unwrap key

This example will pull the secrets stored with the name `sync-creditors` from a namespace called `likvido-api` in a cluster with the kubectl context name `staging`, and then unwrap the key `app.json` and output the value of this key to a file called `appsettings.Development.json`:
```
kubesec pull sync-creditors --namespace likvido-api --context staging --unwrap-key app.json --output appsettings.Development.json
```

### Port forwarding

This example will pull the secrets stored with the name `sync-creditors` from a namespace called `likvido-api` in a cluster with the kubectl context name `staging`, and then unwrap the key `app.json` and output the value of this key to a file called `appsettings.Development.json`. In addition to this, it will look through all URLs in the value and detect any URLs that point to "local" Kubernetes services in your cluster with this format: `http://{service-name}.{namespace}.svc.cluster.local{optional port}`. For all of the URLs it finds, it will parse them and set up port forwarding to them on your localhost. After setting up port forwarding, it will replace the URLs in the outputted text, such that they point to the localhost port forward:
```
kubesec pull sync-creditors --namespace likvido-api --context staging --unwrap-key app.json --port-forward --output appsettings.Development.json
```

### Remove JSON field

In case your secret value is a JSON object, and you would like to remove specific fields from the object before you write the file to the output, then you can do this with the `--remove-json-field` option. This example will pull the secrets stored with the name `sync-creditors` from a namespace called `likvido-api` in a cluster with the kubectl context name `staging`, unwrap the key `app.json`, remove the JSON fields `Events.Created.Name` & `ApplicationInsights` and output the resulting value to a file called `appsettings.Development.json`:
```
kubesec pull sync-creditors --namespace likvido-api --context staging --unwrap-key app.json --output appsettings.Development.json --remove-json-field Events.Created.Name --remove-json-field ApplicationInsights
```

So, to be clear, if the value of the `app.json` secret field in the secret looks like this:

```json
{
  "FieldA": "ValueA",
  "ApplicationInsights": {
    "InstrumentationKey": "blabla"
  },
  "Events": {
    "Created": {
      "Type": "something",
      "Name": "The created event"
    }
  }
}
```

The resulting output (which would be written to the `appsettings.Development.json` file in this case), would look like this:

```json
{
  "FieldA": "ValueA",
  "Events": {
    "Created": {
      "Type": "something"
    }
  }
}
```

## How to push secrets

This is the command to push secrets:
```
kubesec push <file> --namespace <kubectl-namespace> --context <kubectl-context-name> --secret <secret-name> --skip-prompts --auto-create-missing-namespace
```

This is an example command that will push the secrets stored in the file `secret.yaml` to a namespace called `likvido-api` in a cluster with the kubectl context name `staging` using the secret name `sync-creditors`:
```
kubesec push secrets.yaml --namespace likvido-api --context staging --secret sync-creditors
```

If you do not specify `--context`, then it will use whatever context is currently active in kubectl\
If you do not specify `--namespace`, then it will use `default` namespace\
If you do not specify `--skip-prompts`, then it will ask you for confirmation before pushing the secrets\
If you do not specify `--auto-create-missing-namespace`, then it will not create the namespace if it does not exist

## File header

When you pull secrets using kubesec and store them in a file, then it will include a file header looking something like this:
```
#######################################
# Context: staging
# Secret: sync-creditors
# Namespace: likvido-api
#######################################
```

If you later need to modify the secrets, and then push the changes, then kubesec will read the header and use that for some sanity checks before actually applying the changes to Kubernetes. If you provide a `--context` and/or `--secret` when calling the `push` command, then kubesec will compare those to the ones in the file header, and if they diverge, it will ask you if you are sure you wish to push the secrets with the context and secret name you specified.

We recommend keeping the header in the file, and not modifying it.

## Backup

To backup all the secrets in your cluster you can use the following command. 

```
kubesec backup --context <kubectl-context-name>
```
There are three more options that you can add to the command:
1. `--namespace <kubectl-namespace>` - Will look for a specific namespace in a cluster
2. `--namespace-include <keyword>` - Will look for namespaces that includes the specified keyword
3. `--namespace-regex <regex>` - Will look for namespaces that matches the specified regex
4. `--excluded-namespaces` - Will exclude the specified namespaces from the backup

Only one of the above namespace options will take effect. The above order also defines their priority.

If you do not specify `--context`, then it will use whatever context is currently active in kubectl.

When using backup command a folder will be created in the directory you run the command from called `Kubesec_Backup_<context>_<datestamp>`. Inside the created folder one or more folders will be created with the name of `namespaces` that satisfy one of the conditions related to `namespace` options in command which will hold secret files defined in kubernetes (like if you were pulling each one manually).

#### Examples

a) This is an example command that will backup secrets from the namespace called `likvido-api` which is part of a cluster with the kubectl context name `staging` 

```
kubesec backup --namespace likvido-api --context staging
```

b) This command will backup secrets from the namespaces that include `likvido` keyword on their names and are part of a cluster with the kubectl context name `staging` 

```
kubesec backup --namespace-includes likvido --context staging
```

c) This command will backup secrets from the namespaces that contain numbers on their names and are part of a cluster with the kubectl context name `staging` 

```
kubesec backup --namespace-regex \d --context staging
```

d) This command will backup secrets from all namespaces except `kube-system` and `prometheus` with the kubectl context name `staging` 

```
kubesec backup --excluded-namespace kube-system --excluded-namespace prometheus --context staging
```
## Restore

To restore the secrets you have created with the backup command, you can run the following command. It will loop through each file in the provided directory and push them in turn

```
kubesec restore <folder> --context <kubectl-context-name> --recursive --skip-prompts --auto-create-missing-namespaces
```

If you do not specify `--context`, then it will use whatever context is currently active in kubectl\
If you do not specify `--recursive`, then it will only restore the files in the folder you specified\
If you do not specify `--skip-prompts`, then it will ask you for confirmation before pushing the secrets\
If you do not specify `--auto-create-missing-namespaces`, then it will not create the namespaces if they do not exist

## Find

Search for secrets containing a specific value across all secrets in the cluster. This is useful for discovering which secrets contain a particular API key, password, or configuration value before rotating it.

```bash
kubesec find <search-term> --context <kubectl-context-name>
```

There are three more options that you can add to the command:
1. `--namespace <kubectl-namespace>` - Will search in a specific namespace
2. `--namespace-includes <keyword>` - Will search in namespaces that include the specified keyword
3. `--namespace-regex <regex>` - Will search in namespaces that match the specified regex

If you do not specify `--context`, then it will use whatever context is currently active in kubectl.

### Examples

a) This command will search for "OpenAI" in all secrets in a cluster with the kubectl context name `production`:

```bash
kubesec find "OpenAI" --context production
```

b) This command will search for an API key in all secrets within the `likvido-api` namespace:

```bash
kubesec find "sk-<your-api-key>" --namespace likvido-api --context production
```

The output shows each matching secret with context lines around the match:

```text
Finding: OpenAI
=============================================

> default/likvidoapp-internalapi
  Key: appsettings.json
  Line 57:
    },
>   "OpenAI": {
      "ApiKey": "sk-..."
    },
```

## Update Value

Replace a specific value across all secrets in the cluster. This is useful for rotating API keys, passwords, or other credentials that are used by multiple services.

```bash
kubesec update-value --old <old-value> --new <new-value> --context <kubectl-context-name>
```

The command will:
1. Create an automatic backup to `kubesec-rollback-<timestamp>/`
2. Search for all secrets containing the old value
3. Show a preview of all changes that will be made
4. Ask for confirmation before applying
5. Apply the changes to the cluster
6. Show rollback instructions

Additional options:
- `--namespace <kubectl-namespace>` - Only update secrets in a specific namespace
- `--namespace-includes <keyword>` - Only update secrets in namespaces that include the specified keyword
- `--namespace-regex <regex>` - Only update secrets in namespaces that match the specified regex
- `--skip-prompts` - Skip the confirmation prompt (useful for CI/CD)
- `--dry-run` - Show what would be changed without making actual changes

If you do not specify `--context`, then it will use whatever context is currently active in kubectl.

### Examples

a) Preview what would be changed when rotating an API key (dry run):

```bash
kubesec update-value --old "sk-oldkey123" --new "sk-newkey456" --context production --dry-run
```

b) Rotate an API key across all secrets:

```bash
kubesec update-value --old "sk-oldkey123" --new "sk-newkey456" --context production
```

c) Rotate an API key only in a specific namespace:

```bash
kubesec update-value --old "sk-oldkey123" --new "sk-newkey456" --namespace likvido-api --context production
```

The output shows a preview of all changes and asks for confirmation:

```text
=============================================
Replace Values
=============================================

Old value: sk-oldkey123
New value: sk-newkey456

Creating backup to kubesec-rollback-20260115-143022/

Finding secrets containing the old value...

Found 3 secret(s) containing the value

> default/likvidoapp-internalapi
  1 occurrence(s) will be replaced
  Line 58:
    - "ApiKey": "sk-oldkey123"
    + "ApiKey": "sk-newkey456"

...

Apply changes? [y/N] y

Updating secrets...
  OK default/likvidoapp-internalapi
  OK default/likvidoapp-webappapi

=============================================
Done! Updated 3 secret(s).
=============================================

Rollback: kubesec restore -c production kubesec-rollback-20260115-143022/
```

If something goes wrong, you can rollback using the restore command with the backup folder that was created:

```bash
kubesec restore kubesec-rollback-20260115-143022/ --context production --recursive
```

## Releasing a new version

To release a new version to NuGet, run through these steps:

1. Update the version number and release notes in the project file `Likvido.Kubesec/Likvido.Kubesec/Likvido.Kubesec.csproj`
2. Run the command: `dotnet pack Likvido.Kubesec/Likvido.Kubesec/Likvido.Kubesec.csproj`
3. Go to https://www.nuget.org/packages/manage/upload and upload the resulting nupkg file
