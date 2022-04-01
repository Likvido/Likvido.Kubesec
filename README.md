# Likvido.Kubesec
Tool to ease the management of Kubernetes secrets

## Requirements
[kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/) needs to be installed and configured for your cluster.

## Installation
Install this tool via dotnet global tools:
```
dotnet tool install --global Likvido.Kubesec
```

https://www.nuget.org/packages/Likvido.Kubesec

## How to pull secrets

This is the command to pull secrets:
```
kubesec pull <secret-name> --namespace <kubectl-namespace> --context <kubectl-context-name> --unwrap-key <key-name> --output <output-file>
```

This example will pull the secrets stored with the name `sync-creditors` from a namespace called `likvido-api` in a cluster with the kubectl context name `staging`, and then output them to a file called `secrets.yaml`:
```
kubesec pull sync-creditors --namespace likvido-api --context staging --output secrets.yaml
```

If you do not specify `--context`, then it will use whatever context is currently active in kubectl
If you do not specify `--namespace`, then it will use `default` namespace
If you do not specify `--output`, then it will display the data inside the console in a table format
If you do not specify `--unwrap-key` then it will fetch all keys and output them with both their key name and value

This example will pull the secrets stored with the name `sync-creditors` from a namespace called `likvido-api` in a cluster with the kubectl context name `staging`, and then unwrap the key `app.json` and output the value of this key to a file called `appsettings.Development.json`:
```
kubesec pull sync-creditors --namespace likvido-api --context staging --unwrap-key app.json --output appsettings.Development.json
```

## How to push secrets

This is the command to push secrets:
```
kubesec push <file> --namespace <kubectl-namespace> --context <kubectl-context-name> --secret <secret-name>
```

This is an example command that will push the secrets stored in the file `secret.yaml` to a namespace called `likvido-api` in a cluster with the kubectl context name `staging` using the secret name `sync-creditors`:
```
kubesec push secrets.yaml --namespace likvido-api --context staging --secret sync-creditors
```

If you do not specify `--context`, then it will use whatever context is currently active in kubectl
If you do not specify `--namespace`, then it will use `default` namespace

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
## Restore

To restore the secrets you have created with the backup command, you can run the following command. It will loop through each file in the provided directory and push them in turn

```
kubesec restore <folder> --context <kubectl-context-name>
```

If you do not specify `--context`, then it will use whatever context is currently active in kubectl

## Releasing a new version

To release a new version to NuGet, run through these steps:

1. Update the version number and release noted in the project file `Likvido.Kubesec/Likvido.Kubesec/Likvido.Kubesec.csproj`
2. Run the command: `dotnet pack Likvido.Kubesec/Likvido.Kubesec/Likvido.Kubesec.csproj`
3. Go to https://www.nuget.org/packages/manage/upload and upload the resulting nupkg file