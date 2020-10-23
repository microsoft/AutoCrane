// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace AutoCrane.Apps
{
    internal static class GenerateYaml
    {
        private const string Yaml = @"
apiVersion: v1
kind: Namespace
metadata:
  name: !!NAMESPACE!!
---
kind: ClusterRole
apiVersion: rbac.authorization.k8s.io/v1beta1
metadata:
  namespace: !!NAMESPACE!!
  name: pod-reader-writer
rules:
- apiGroups: [""""]
  resources: [""pods""]
  verbs: [""get"", ""list"", ""patch""]
---
kind: ClusterRole
apiVersion: rbac.authorization.k8s.io/v1beta1
metadata:
  namespace: !!NAMESPACE!!
  name: pod-eviction
rules:
- apiGroups: [""""]
  resources: [""pods/eviction""]
  verbs: [""create""]
---
kind: ClusterRoleBinding
apiVersion: rbac.authorization.k8s.io/v1beta1
metadata:
  name: pod-reader-writer-binding-autocrane
subjects:
- kind: ServiceAccount
  name: autocrane
  namespace: !!NAMESPACE!!
roleRef:
  kind: ClusterRole
  name: pod-reader-writer
  apiGroup: rbac.authorization.k8s.io
---
kind: ClusterRoleBinding
apiVersion: rbac.authorization.k8s.io/v1beta1
metadata:
  name: pod-eviction-binding
subjects:
- kind: ServiceAccount
  name: autocrane
  namespace: !!NAMESPACE!!
roleRef:
  kind: ClusterRole
  name: pod-eviction
  apiGroup: rbac.authorization.k8s.io
---
apiVersion: v1
kind: ServiceAccount
metadata:
  name: autocrane
  namespace: !!NAMESPACE!!
  labels:
    app.kubernetes.io/name: autocrane
    app.kubernetes.io/part-of: autocrane
---
apiVersion: policy/v1beta1
kind: PodDisruptionBudget
metadata:
  namespace: !!NAMESPACE!!
  name: autocrane-pdb
spec:
  minAvailable: 1
  selector:
    matchLabels:
      app.kubernetes.io/name: autocrane
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: autocrane
  namespace: !!NAMESPACE!!
  labels:
    app.kubernetes.io/name: autocrane
    app.kubernetes.io/part-of: autocrane
spec:
  selector:
    matchLabels:
      app.kubernetes.io/name: autocrane
  replicas: !!AUTOCRANE_REPLICAS!!
  template:
    metadata:
      labels:
        app.kubernetes.io/name: autocrane
        app.kubernetes.io/part-of: autocrane
    spec:
      containers:
      - name: autocrane
        image: !!IMAGE!!
        imagePullPolicy: !!PULL!!
        env:
          - name: AUTOCRANE_ARGS
            value: orchestrate
          - name: AutoCrane__Namespaces
            value: !!AUTOCRANE_NAMESPACES!!
        resources:
          requests:
            cpu: 100m
            memory: 50M
      serviceAccountName: autocrane
      nodeSelector:
        beta.kubernetes.io/os: linux
";

        private const string WatchdogListenerYaml = @"
---
apiVersion: v1
kind: ServiceAccount
metadata:
  name: watchdoglistener
  namespace: !!NAMESPACE!!
  labels:
    app.kubernetes.io/name: watchdoglistener
    app.kubernetes.io/part-of: autocrane
---
kind: ClusterRoleBinding
apiVersion: rbac.authorization.k8s.io/v1beta1
metadata:
  name: pod-reader-writer-binding-watchdoglistener
subjects:
- kind: ServiceAccount
  name: watchdoglistener
  namespace: !!NAMESPACE!!
roleRef:
  kind: ClusterRole
  name: pod-reader-writer
  apiGroup: rbac.authorization.k8s.io
---
apiVersion: policy/v1beta1
kind: PodDisruptionBudget
metadata:
  namespace: !!NAMESPACE!!
  name: watchdoglistener-pdb
spec:
  minAvailable: 1
  selector:
    matchLabels:
      app.kubernetes.io/name: watchdoglistener
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: watchdoglistener
  namespace: !!NAMESPACE!!
  labels:
    app.kubernetes.io/name: watchdoglistener
    app.kubernetes.io/part-of: autocrane
spec:
  selector:
    matchLabels:
      app.kubernetes.io/name: watchdoglistener
  replicas: !!WATCHDOGLISTENER_REPLICAS!!
  template:
    metadata:
      labels:
        app.kubernetes.io/name: watchdoglistener
        app.kubernetes.io/part-of: autocrane
    spec:
      containers:
      - name: watchdoglistener
        image: !!IMAGE!!
        imagePullPolicy: !!PULL!!
        ports:
        - containerPort: 8080
          name: http
        env:
          - name: AUTOCRANE_ARGS
            value: watchdoglistener
          - name: AutoCrane__Namespaces
            value: !!AUTOCRANE_NAMESPACES!!
        resources:
          requests:
            cpu: 100m
            memory: 50M
        livenessProbe:
          httpGet:
            path: /ping
            port: http
          initialDelaySeconds: 20
          periodSeconds: 60
          timeoutSeconds: 10
        readinessProbe:
          httpGet:
            path: /ping
            port: http
          initialDelaySeconds: 10
          periodSeconds: 15
          timeoutSeconds: 10
      serviceAccountName: watchdoglistener
      nodeSelector:
        beta.kubernetes.io/os: linux

";

        private const string WatchdogProberYaml = @"
---
apiVersion: v1
kind: ServiceAccount
metadata:
  name: watchdogprober
  namespace: !!NAMESPACE!!
  labels:
    app.kubernetes.io/name: watchdogprober
    app.kubernetes.io/part-of: autocrane
---
kind: ClusterRoleBinding
apiVersion: rbac.authorization.k8s.io/v1beta1
metadata:
  name: pod-reader-writer-binding-watchdogprober
subjects:
- kind: ServiceAccount
  name: watchdogprober
  namespace: !!NAMESPACE!!
roleRef:
  kind: ClusterRole
  name: pod-reader-writer
  apiGroup: rbac.authorization.k8s.io
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: watchdogprober
  namespace: !!NAMESPACE!!
  labels:
    app.kubernetes.io/name: watchdogprober
    app.kubernetes.io/part-of: autocrane
spec:
  selector:
    matchLabels:
      app.kubernetes.io/name: watchdogprober
  replicas: !!WATCHDOGPROBER_REPLICAS!!
  template:
    metadata:
      labels:
        app.kubernetes.io/name: watchdogprober
        app.kubernetes.io/part-of: autocrane
    spec:
      containers:
      - name: watchdogprober
        image: !!IMAGE!!
        imagePullPolicy: !!PULL!!
        ports:
        - containerPort: 8080
          name: http
        env:
          - name: AUTOCRANE_ARGS
            value: watchdogprober
          - name: AutoCrane__Namespaces
            value: !!AUTOCRANE_NAMESPACES!!
        resources:
          requests:
            cpu: 100m
            memory: 50M
      serviceAccountName: watchdogprober
      nodeSelector:
        beta.kubernetes.io/os: linux

";

        private const string TestWorkloadYaml = @"
---
apiVersion: v1
kind: ServiceAccount
metadata:
  name: testworkload
  namespace: !!NAMESPACE!!
  labels:
    app.kubernetes.io/name: testworkload
    app.kubernetes.io/part-of: autocrane
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: testworkload
  namespace: !!NAMESPACE!!
  labels:
    app.kubernetes.io/name: testworkload
    app.kubernetes.io/part-of: autocrane
spec:
  selector:
    matchLabels:
      app.kubernetes.io/name: testworkload
  replicas: !!TESTWORKLOAD_REPLICAS!!
  template:
    metadata:
      annotations:
        probe.autocrane.io/watchdog1: POD_IP:8080/watchdog
      labels:
        app.kubernetes.io/name: testworkload
        app.kubernetes.io/part-of: autocrane
    spec:
      containers:
      - name: testworkload
        image: !!IMAGE!!
        imagePullPolicy: !!PULL!!
        ports:
        - containerPort: 8080
          name: http
        env:
          - name: AUTOCRANE_ARGS
            value: testworkload
        resources:
          requests:
            cpu: 100m
            memory: 50M
        livenessProbe:
          httpGet:
            path: /ping
            port: http
          initialDelaySeconds: 20
          periodSeconds: 60
          timeoutSeconds: 10
        readinessProbe:
          httpGet:
            path: /ping
            port: http
          initialDelaySeconds: 10
          periodSeconds: 15
          timeoutSeconds: 10
      serviceAccountName: testworkload
      nodeSelector:
        beta.kubernetes.io/os: linux

";

        public static int Run(string[] args)
        {
            var config = new Dictionary<string, string>()
            {
                ["namespace"] = "autocrane",
                ["image"] = "autocrane",
                ["pull"] = "Never", // for local development
                ["autocrane_namespaces"] = "autocrane", // namespaces to operate in
                ["autocrane_replicas"] = "1",
                ["watchdoglistener_replicas"] = "3",
                ["watchdogprober_replicas"] = "1",
                ["testworkload_replicas"] = "3",
                ["use_watchdoglistener"] = "0",
                ["use_watchdogprober"] = "1",
                ["use_testworkload"] = "0",
            };

            foreach (var arg in args)
            {
                var splits = arg.Split('=', 2);
                if (splits.Length == 2)
                {
                    var key = splits[0];
                    var val = splits[1];
                    if (!config.ContainsKey(key))
                    {
                        Console.Error.WriteLine($"Could not find config key: {key}");
                        return 1;
                    }
                    else
                    {
                        config[key] = val;
                    }
                }
            }

            var output = Yaml.Replace("\r", string.Empty);
            if (config["use_watchdoglistener"] != "0")
            {
                output += WatchdogListenerYaml.Replace("\r", string.Empty);
            }

            if (config["use_testworkload"] != "0")
            {
                output += TestWorkloadYaml.Replace("\r", string.Empty);
            }

            if (config["use_watchdogprober"] != "0")
            {
                output += WatchdogProberYaml.Replace("\r", string.Empty);
            }

            foreach (var item in config)
            {
                output = output.Replace($"!!{item.Key.ToUpperInvariant()}!!", item.Value);
            }

            Console.WriteLine(output);
            return 0;
        }
    }
}
