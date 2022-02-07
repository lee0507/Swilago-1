import { Injectable } from "@angular/core";
import { DeviceDetectorService } from "ngx-device-detector";

@Injectable()
export class DeviceCheckService {
  deviceInfo = null;

  constructor(private deviceService: DeviceDetectorService) {

  }

  epicFunction() {
    this.deviceInfo = this.deviceService.getDeviceInfo();
    const isMobile = this.deviceService.isMobile();
    const isTablet = this.deviceService.isTablet();
    const isDesktopDevice = this.deviceService.isDesktop();


    if (isMobile) {
      return 'mobile'
    } else if (isTablet) {
      return 'tablet'
    } else if (isDesktopDevice) {
      return 'desktop'
    }
  }
}
