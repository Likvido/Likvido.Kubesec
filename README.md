# Likvido.Confocto
Tool to ease the management of Kubernetes secrets

# Requirements
To pull secrets from a Kubernetes cluster, you first need to have the [kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/) tool installed and configured for your cluster.

# How to pull secrets

This is the command to pull secrets:
```
confocto pull <secret-name> --context <kubectl-context-name> --output <output-file>
```

This example will pull the secrets stored with the name `sync-creditors` in a cluster with the kubectl context name `staging-windows`, and then output them to a file called `secrets.env`:
```
confocto pull sync-creditors --context staging-windows --output secrets.env
```

If you do not specify `--context`, then it will use whatever context is currently active in kubectl
If you do not specify `--output`, then it will display the data inside the console in a table format

# How to push secrets

This is the command to push secrets:
```
confocto push <file> --context <kubectl-context-name> --secret <secret-name>
```

This is an example command that will push the secrets stored in the file `secret.env` to the cluster with the kubectl context name `staging-windows` using the secret name `sync-creditors`:
```
confocto push secrets.env --context staging-windows --secret sync-creditors
```

If you do not specify `--context`, then it will use whatever context is currently active in kubectl

# File header

When you pull secrets using confocto and store them in a file, then it will include a file header looking something like this:
```
#######################################
# Context: staging-windows
# Secret: sync-creditors
#######################################
```

If you later need to modify the secrets, and then push the changes, then confocto will read the header and use that for some sanity checks before actually applying the changes to Kubernetes. If you provide a `--context` and/or `--secret` when calling the `push` command, then confocto will compare those to the ones in the file header, and if they diverge, it will ask you if you are sure you wish to push the secrets with the context and secret name you specified.

We recommend keeping the header in the file, and not modifying it.
