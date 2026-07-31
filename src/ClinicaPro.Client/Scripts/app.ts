interface ClinicaProBrowserApi {
    setPageTitle: (title: string) => void;
}

const browserWindow = window as Window & { clinicaPro?: ClinicaProBrowserApi };

browserWindow.clinicaPro = {
    setPageTitle(title: string): void {
        document.title = title;
    }
};
