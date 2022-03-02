# Linz AG, Linz Netz Verbrauchsdaten

This project aims at automatically getting power consumption information from Linz Strom AG ("Verbrauchsdateninformation" from https://services.linznetz.at/).

It uses a headless Chrome to log in and download consumption data as CSV for all appliances.

The data can be directly inserted into an InfluxDb as measurements.

If you want to try, use the `LinzNetzConsumptionToInfluxDb` app.

## Run in Kubernetes

CronJob example
```
apiVersion: batch/v1
kind: CronJob
metadata:
  name: linzstrom
spec:
  schedule: "0 * * * *" # every hour
  jobTemplate:
    spec:
      template:
        spec:
          containers:
          - name: linzstrom
            image: docker.io/discostu105/linznetzconsumptiontoinfluxdb:20220227134744 # https://hub.docker.com/r/discostu105/linznetzconsumptiontoinfluxdb/tags
            imagePullPolicy: IfNotPresent
            command: ["dotnet"]
            args: ["LinzNetzConsumptionToInfluxDb.dll", "--days", "3", "--username", "xxx@gmail.com", "--password", "***", "--influxendpoint", "http://influxdb:8086", "--influxtoken", "***"]
          restartPolicy: Never
          nodeSelector:
            kubernetes.io/arch: amd64
      backoffLimit: 3
```

Example output log:
```
Chrome ready
go to login page
enter credentials
click login submit
login done!
go to Verbrauchsdateninformation
BaseInfo { Address = xxx, anlagen = xxx }
Anlage { name = Basisanlage, zaehlerNummer = xxx, zaehlPunktNummer = AT003100000000000000000xxx, id = plant-41xxx }
Anlage { name = Wärmepumpe Kombi, zaehlerNummer = yyy, zaehlPunktNummer = AT0031000000000000000yyy, id = plant-41yyy }
Selecting anlage plant-41xxx
Selecting Viertelstunden
Setting fromdate to 19.02.2022
Setting todate to 27.02.2022
load table
table load finished
csv export
export finished
writing 672 csv records
first: 19 Feb 2022 00:00:00 19 Feb 2022 00:15:00 0,138
last: 25 Feb 2022 23:45:00 26 Feb 2022 00:00:00 0,137
Selecting anlage plant-41yyy
Selecting Viertelstunden
Setting fromdate to 19.02.2022
Setting todate to 27.02.2022
load table
table load finished
csv export
export finished
writing 672 csv records
first: 19 Feb 2022 00:00:00 19 Feb 2022 00:15:00 0,004
last: 25 Feb 2022 23:45:00 26 Feb 2022 00:00:00 0,236
```

Grafana Example:
![image](https://user-images.githubusercontent.com/10918780/155862787-4891b856-121e-4694-b148-9c169c0c2a34.png)

## Disclaimer

Of course this is very fragile, as every UI change from LINZ AG would break functionality of this crawler. If someone from LINZ AG reads this: A REST API would be very much appreciated and much easier to consume :).
