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
kubesec pull <secret-name> --context <kubectl-context-name> --output <output-file>
```

This example will pull the secrets stored with the name `sync-creditors` in a cluster with the kubectl context name `staging-win`, and then output them to a file called `secrets.env`:
```
kubesec pull sync-creditors --context staging-win --output secrets.env
```

If you do not specify `--context`, then it will use whatever context is currently active in kubectl
If you do not specify `--output`, then it will display the data inside the console in a table format

## How to push secrets

This is the command to push secrets:
```
kubesec push <file> --context <kubectl-context-name> --secret <secret-name>
```

This is an example command that will push the secrets stored in the file `secret.env` to the cluster with the kubectl context name `staging-win` using the secret name `sync-creditors`:
```
kubesec push secrets.env --context staging-win --secret sync-creditors
```

If you do not specify `--context`, then it will use whatever context is currently active in kubectl

## File header

When you pull secrets using kubesec and store them in a file, then it will include a file header looking something like this:
```
#######################################
# Context: staging-win
# Secret: sync-creditors
#######################################
```

If you later need to modify the secrets, and then push the changes, then kubesec will read the header and use that for some sanity checks before actually applying the changes to Kubernetes. If you provide a `--context` and/or `--secret` when calling the `push` command, then confocto will compare those to the ones in the file header, and if they diverge, it will ask you if you are sure you wish to push the secrets with the context and secret name you specified.

We recommend keeping the header in the file, and not modifying it.

## Backup

To backup all the secrets in your cluster you can the following command. It will create a folder in the directory you run the command from called `Kubesec_Backup_<context>_<datestamp>` and inside this folder it will create one file for each secret defined in kubernetes (like if you were pulling each one manually)

```
kubesec backup --context <kubectl-context-name>
```

If you do not specify `--context`, then it will use whatever context is currently active in kubectl

## Restore

To restore the secrets you have created with the backup command, you can run the following command. It will loop through each file in the provided directory and push them in turn

```
kubesec restore <folder> --context <kubectl-context-name>
```

If you do not specify `--context`, then it will use whatever context is currently active in kubectl
