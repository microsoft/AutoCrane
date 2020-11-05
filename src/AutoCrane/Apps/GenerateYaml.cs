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
apiVersion: v1
kind: ServiceAccount
metadata:
  name: autocrane
  namespace: !!NAMESPACE!!
  labels:
    app.kubernetes.io/name: autocrane
    app.kubernetes.io/part-of: autocrane
---
kind: Role
apiVersion: rbac.authorization.k8s.io/v1
metadata:
  namespace: !!NAMESPACE!!
  name: autocrane-pod-reader
  labels:
    app.kubernetes.io/name: autocrane
    app.kubernetes.io/part-of: autocrane
rules:
- apiGroups: [""""]
  resources: [""pods""]
  verbs: [""get"", ""list""]
---
kind: Role
apiVersion: rbac.authorization.k8s.io/v1
metadata:
  namespace: !!NAMESPACE!!
  name: autocrane-pod-reader-writer
  labels:
    app.kubernetes.io/name: autocrane
    app.kubernetes.io/part-of: autocrane
rules:
- apiGroups: [""""]
  resources: [""pods""]
  verbs: [""get"", ""list"", ""patch""]
---
kind: Role
apiVersion: rbac.authorization.k8s.io/v1
metadata:
  namespace: !!NAMESPACE!!
  name: autocrane-pod-eviction
  labels:
    app.kubernetes.io/name: autocrane
    app.kubernetes.io/part-of: autocrane
rules:
- apiGroups: [""""]
  resources: [""pods/eviction""]
  verbs: [""create""]
---
kind: RoleBinding
apiVersion: rbac.authorization.k8s.io/v1
metadata:
  name: autocrane-pod-reader-writer-binding-autocrane
  namespace: !!NAMESPACE!!
  labels:
    app.kubernetes.io/name: autocrane
    app.kubernetes.io/part-of: autocrane
subjects:
- kind: ServiceAccount
  name: autocrane
  namespace: !!NAMESPACE!!
roleRef:
  kind: Role
  name: autocrane-pod-reader-writer
  apiGroup: rbac.authorization.k8s.io
---
kind: RoleBinding
apiVersion: rbac.authorization.k8s.io/v1
metadata:
  name: autocrane-pod-eviction-binding-autocrane
  namespace: !!NAMESPACE!!
  labels:
    app.kubernetes.io/name: autocrane
    app.kubernetes.io/part-of: autocrane
subjects:
- kind: ServiceAccount
  name: autocrane
  namespace: !!NAMESPACE!!
roleRef:
  kind: Role
  name: autocrane-pod-eviction
  apiGroup: rbac.authorization.k8s.io
---
apiVersion: policy/v1beta1
kind: PodDisruptionBudget
metadata:
  namespace: !!NAMESPACE!!
  name: autocrane-pdb
  labels:
    app.kubernetes.io/name: autocrane
    app.kubernetes.io/part-of: autocrane
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
            valueFrom:
              fieldRef:
                fieldPath: metadata.namespace
        resources:
          requests:
            cpu: !!CPU!!
            memory: 50M
      serviceAccountName: autocrane
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
kind: RoleBinding
apiVersion: rbac.authorization.k8s.io/v1
metadata:
  name: autocrane-pod-reader-writer-binding-watchdogprober
  namespace: !!NAMESPACE!!
  labels:
    app.kubernetes.io/name: watchdogprober
    app.kubernetes.io/part-of: autocrane
subjects:
- kind: ServiceAccount
  name: watchdogprober
  namespace: !!NAMESPACE!!
roleRef:
  kind: Role
  name: autocrane-pod-reader-writer
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
            valueFrom:
              fieldRef:
                fieldPath: metadata.namespace
        resources:
          requests:
            cpu: !!CPU!!
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
kind: RoleBinding
apiVersion: rbac.authorization.k8s.io/v1
metadata:
  name: testworkload-rolebinding
  namespace: !!NAMESPACE!!
  labels:
    app.kubernetes.io/name: testworkload
    app.kubernetes.io/part-of: autocrane
subjects:
- kind: ServiceAccount
  name: testworkload
  namespace: !!NAMESPACE!!
roleRef:
  kind: Role
  name: autocrane-pod-reader
  apiGroup: rbac.authorization.k8s.io
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
        probe.autocrane.io/datadeploy: POD_IP:8082/watchdog
        store.autocrane.io/location: /data
        data.autocrane.io/data1: autocranegit
        data.autocrane.io/data2: data2
      labels:
        app.kubernetes.io/name: testworkload
        app.kubernetes.io/part-of: autocrane
    spec:
      volumes:
          - name: test-volume
            emptyDir: {}
      initContainers:
          - name: init1
            image: !!IMAGE!!
            imagePullPolicy: !!PULL!!
            volumeMounts:
                - mountPath: /data
                  name: test-volume
            env:
              - name: AUTOCRANE_ARGS
                value: datadeployinit
              - name: Pod__Name
                valueFrom:
                  fieldRef:
                    fieldPath: metadata.name
              - name: Pod__Namespace
                valueFrom:
                  fieldRef:
                    fieldPath: metadata.namespace
      containers:
      - name: testworkload
        image: !!IMAGE!!
        imagePullPolicy: !!PULL!!
        volumeMounts:
            - mountPath: /data
              name: test-volume
        ports:
          - containerPort: 8080
            name: http
        env:
          - name: AUTOCRANE_ARGS
            value: testworkload
        resources:
          requests:
            cpu: !!CPU!!
            memory: 50M
        livenessProbe:
          httpGet:
            path: /ping
            port: http
          periodSeconds: 60
          timeoutSeconds: 10
        startupProbe:
          httpGet:
            path: /ping
            port: http
          failureThreshold: 30
        readinessProbe:
          httpGet:
            path: /ping
            port: http
          initialDelaySeconds: 10
          periodSeconds: 15
          timeoutSeconds: 10
      - name: data
        image: !!IMAGE!!
        imagePullPolicy: !!PULL!!
        volumeMounts:
            - mountPath: /data
              name: test-volume
        ports:
          - containerPort: 8082
            name: watchdog
        readinessProbe:
          httpGet:
            path: /watchdog
            port: watchdog
          failureThreshold: 1
          periodSeconds: 20
          timeoutSeconds: 10
        env:
          - name: AUTOCRANE_ARGS
            value: datadeploy
          - name: LISTEN_PORT
            value: ""8082""
          - name: Pod__Name
            valueFrom:
              fieldRef:
                fieldPath: metadata.name
          - name: Pod__Namespace
            valueFrom:
              fieldRef:
                fieldPath: metadata.namespace
      - name: watchdoghealthz
        image: !!IMAGE!!
        imagePullPolicy: !!PULL!!
        ports:
          - containerPort: 8081
            name: watchdog
        env:
          - name: AUTOCRANE_ARGS
            value: watchdoghealthz
          - name: LISTEN_PORT
            value: ""8081""
          - name: Watchdogs__AlwaysHealthyAfterSeconds
            value: ""600""
          - name: Pod__Name
            valueFrom:
              fieldRef:
                fieldPath: metadata.name
          - name: Pod__Namespace
            valueFrom:
              fieldRef:
                fieldPath: metadata.namespace
        resources:
          requests:
            cpu: !!CPU!!
            memory: 50M
        readinessProbe:
          httpGet:
            path: /healthz
            port: watchdog
          failureThreshold: 1
          periodSeconds: 20
          timeoutSeconds: 10
      serviceAccountName: testworkload
      nodeSelector:
        beta.kubernetes.io/os: linux
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: testworkloadnoinit
  namespace: !!NAMESPACE!!
  labels:
    app.kubernetes.io/name: testworkload2
    app.kubernetes.io/part-of: autocrane
spec:
  selector:
    matchLabels:
      app.kubernetes.io/name: testworkload2
  replicas: !!TESTWORKLOAD_REPLICAS!!
  template:
    metadata:
      annotations:
        probe.autocrane.io/watchdog1: POD_IP:8080/watchdog
        probe.autocrane.io/datadeploy: POD_IP:8082/watchdog
        store.autocrane.io/location: /data
        data.autocrane.io/data1: autocranegit
      labels:
        app.kubernetes.io/name: testworkload2
        app.kubernetes.io/part-of: autocrane
    spec:
      volumes:
          - name: test-volume
            emptyDir: {}
      containers:
      - name: testworkload
        image: !!IMAGE!!
        imagePullPolicy: !!PULL!!
        volumeMounts:
            - mountPath: /data
              name: test-volume
        ports:
          - containerPort: 8080
            name: http
        env:
          - name: AUTOCRANE_ARGS
            value: testworkload
        resources:
          requests:
            cpu: !!CPU!!
            memory: 50M
        livenessProbe:
          httpGet:
            path: /ping
            port: http
          periodSeconds: 60
          timeoutSeconds: 10
        startupProbe:
          httpGet:
            path: /ping
            port: http
          failureThreshold: 30
        readinessProbe:
          httpGet:
            path: /ping
            port: http
          initialDelaySeconds: 10
          periodSeconds: 15
          timeoutSeconds: 10
      - name: data
        image: !!IMAGE!!
        imagePullPolicy: !!PULL!!
        volumeMounts:
            - mountPath: /data
              name: test-volume
        env:
          - name: AUTOCRANE_ARGS
            value: datadeploy
          - name: LISTEN_PORT
            value: ""8082""
          - name: Pod__Name
            valueFrom:
              fieldRef:
                fieldPath: metadata.name
          - name: Pod__Namespace
            valueFrom:
              fieldRef:
                fieldPath: metadata.namespace
        ports:
          - containerPort: 8082
            name: watchdog
        readinessProbe:
          httpGet:
            path: /watchdog
            port: watchdog
          failureThreshold: 1
          periodSeconds: 20
          timeoutSeconds: 10
        resources:
          requests:
            cpu: !!CPU!!
            memory: 50M
      serviceAccountName: testworkload
      nodeSelector:
        beta.kubernetes.io/os: linux
";

        private const string DataRepoYaml = @"
---
apiVersion: v1
kind: ServiceAccount
metadata:
  name: datarepo
  namespace: !!NAMESPACE!!
  labels:
    app.kubernetes.io/name: datarepo
    app.kubernetes.io/part-of: autocrane
---
apiVersion: v1
kind: Service
metadata:
  name: datarepo
  namespace: !!NAMESPACE!!
  labels:
    app.kubernetes.io/name: datarepo
    app.kubernetes.io/part-of: autocrane
spec:
  ports:
  - name: http
    port: 80
    protocol: TCP
    targetPort: http
  selector:
    app.kubernetes.io/name: datarepo
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: datarepo
  namespace: !!NAMESPACE!!
  labels:
    app.kubernetes.io/name: datarepo
    app.kubernetes.io/part-of: autocrane
spec:
  selector:
    matchLabels:
      app.kubernetes.io/name: datarepo
  replicas: !!DATAREPO_REPLICAS!!
  template:
    metadata:
      labels:
        app.kubernetes.io/name: datarepo
        app.kubernetes.io/part-of: autocrane
    spec:
      volumes:
          - name: data-store
            emptyDir: {}
      containers:
      - name: datarepo
        image: !!IMAGE!!
        imagePullPolicy: !!PULL!!
        volumeMounts:
            - mountPath: /data
              name: data-store
        ports:
        - containerPort: 8080
          name: http
        env:
          - name: AUTOCRANE_ARGS
            value: datarepo
          - name: DataRepo__ArchivePath
            value: /data/archives
          - name: DataRepo__SourcePath
            value: /data/source
          - name: DataRepo__Sources
            value: ""!!DATAREPO_SOURCES!!""
        resources:
          requests:
            cpu: !!CPU!!
            memory: 50M
        livenessProbe:
          httpGet:
            path: /ping
            port: http
          periodSeconds: 60
          timeoutSeconds: 10
        startupProbe:
          httpGet:
            path: /ping
            port: http
          failureThreshold: 10
        readinessProbe:
          httpGet:
            path: /ping
            port: http
          initialDelaySeconds: 10
          periodSeconds: 15
          timeoutSeconds: 10
      serviceAccountName: datarepo
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
                ["cpu"] = "10m",
                ["autocrane_replicas"] = "1",
                ["watchdogprober_replicas"] = "1",
                ["testworkload_replicas"] = "2",
                ["datarepo_replicas"] = "1",
                ["use_watchdogprober"] = "1",
                ["use_testworkload"] = "0",
                ["use_datarepo"] = "1",
                ["datarepo_sources"] = "autocranegit:git@https://github.com/microsoft/AutoCrane.git;data2:git@https://github.com/microsoft/AutoCrane.git",
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

            if (config["use_testworkload"] != "0")
            {
                output += TestWorkloadYaml.Replace("\r", string.Empty);
            }

            if (config["use_watchdogprober"] != "0")
            {
                output += WatchdogProberYaml.Replace("\r", string.Empty);
            }

            if (config["use_datarepo"] != "0")
            {
                output += DataRepoYaml.Replace("\r", string.Empty);
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
