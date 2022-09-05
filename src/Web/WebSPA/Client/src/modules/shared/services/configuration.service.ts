import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from "@angular/common/http";
import { IConfiguration }   from '../models/configuration.model';
import { StorageService }   from './storage.service';

import { Observable, Subject } from 'rxjs';

@Injectable()
export class ConfigurationService {
    serverSettings: IConfiguration;
    // observable that is fired when settings are loaded from server
    private settingsLoadedSource = new Subject();
    settingsLoaded$ = this.settingsLoadedSource.asObservable();
    isReady: boolean = false;

    constructor(private http: HttpClient, private storageService: StorageService) { }

    load() {
        const baseURI = document.baseURI.endsWith('/') ? document.baseURI : `${document.baseURI}/`;
         let url = `${baseURI}Home/Configuration`;
         this.http.get(url).subscribe((response) => {
             console.log('server settings loaded');
             this.serverSettings = response as IConfiguration;
             console.log(this.serverSettings);
             this.storageService.store('identityUrl', this.serverSettings.identityUrl);
             this.storageService.store('purchaseUrl', this.serverSettings.purchaseUrl);
             this.storageService.store('signalrHubUrl', this.serverSettings.signalrHubUrl);
             this.storageService.store('activateCampaignDetailFunction', this.serverSettings.activateCampaignDetailFunction);
             this.isReady = true;
             this.settingsLoadedSource.next();
         });
            //setTimeout(() => {
            //    this.serverSettings = JSON.parse("{\r\n    \"identityUrl\": \"http:\\/\\/host.docker.internal:5105\",\r\n    \"callBackUrl\": \"http:\\/\\/host.docker.internal:5104\\/\",\r\n    \"basketUrl\": \"http:\\/\\/host.docker.internal:5103\",\r\n    \"purchaseUrl\": \"http:\\/\\/host.docker.internal:5202\",\r\n    \"purchaseUrlHc\": \"http:\\/\\/host.docker.internal:5202\\/hc\",\r\n    \"identityUrlHc\": \"http:\\/\\/host.docker.internal:5105\\/hc\",\r\n    \"signalrHubUrl\": \"http:\\/\\/host.docker.internal:5112\",\r\n    \"useCustomizationData\": true,\r\n    \"isClusterEnv\": \"False\",\r\n    \"activateCampaignDetailFunction\": false,\r\n    \"logging\": {\r\n        \"includeScopes\": false,\r\n        \"logLevel\": {\r\n            \"default\": \"Debug\",\r\n            \"system\": \"Information\",\r\n            \"microsoft\": \"Information\"\r\n        }\r\n    },\r\n    \"applicationInsights\": {\r\n        \"instrumentationKey\": \"\"\r\n    }\r\n}") as IConfiguration;
            //    this.storageService.store('identityUrl', 'http://host.docker.internal:5105');
            //    this.storageService.store('purchaseUrl', 'http://host.docker.internal:5202');
            //    this.storageService.store('signalrHubUrl', 'http://host.docker.internal:5112');
            //    this.storageService.store('activateCampaignDetailFunction', false);
            //    this.isReady = true;
            //    this.settingsLoadedSource.next();
            //}, 1000);
            
    }
}
