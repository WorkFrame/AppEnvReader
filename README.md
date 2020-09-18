# AppEnvReader
Verwaltet eine Liste von IGetStringValue-Objekten und fragt diese der Reihe nach ab um das erste gültige Ergebnis selbst wieder als IGetStringValue an den Aufrufer zurück zu geben.
Wird von BasicAppSettings benutzt, um Konfigurationseinstellungen aus diversen Quellen einzulesen (siehe bei BasicAppSettings).
Für Values, die Wildcards der Form '%Name%' enthalten, findet eine rekursive Ersetzung statt (nur für GetStringValue(...)).
Verwaltet eine zusätzliche Liste, die von außen mit Key-Value Paaren gefüllt werden kann; diese Liste wird bei der Suche ebenfalls berücksichtigt. 
