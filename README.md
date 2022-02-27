# Linz AG, Linz Netz Verbrauchsdaten

This project aims at automatically getting power consumption information from Linz Strom AG (https://services.linznetz.at/verbrauchsdateninformation).

It uses a headless Chrome to log in and download consumption data as CSV for all appliances.

The data can be directly inserted into an InfluxDb as measurements.

If you want to try, use the LinzStromFetcher.

To get the picture, here is an output log:
```
Chrome ready
go to login page
enter credentials
click login submit
login done!
go to Verbrauchsdateninformation
BaseInfo { Address = xxx, anlagen = xxx }
Anlage { name = Basisanlage, zaehlerNummer = xxx, zaehlPunktNummer = AT003100000000000000000xxx, id = plant-41xxx }
Anlage { name = Wärmepumpe Kombi, zaehlerNummer = xxx, zaehlPunktNummer = AT00310000000000xxx, id = plant-41xxx }
Selecting anlage plant-41465
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
Selecting anlage plant-41466
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
