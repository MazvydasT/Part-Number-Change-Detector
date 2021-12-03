# Part Number Change Detector

To use in GUI mode simply start application without any arguments.

## Command line usage

### To compare against multiple previous reports:
 
    PNCD -r=EBOM_Report.csv
         -e=eMS_EBOM_Report.csv
         -p=previous_eMS_EBOM_Report_1.csv
         -p=previous_eMS_EBOM_Report_2.csv
         -d=ds_list.csv

### To output ds_list.csv by redirection:

    PNCD -r EBOM_Report.csv
         -e eMS_EBOM_Report.csv
         -p "previous eMS EBOM Report.csv"
         > ds_list.csv

### To output DS list to console:

    PNCD -r "EBOM Report.csv"
         -e "eMS EBOM Report.csv"
         -p=previous_eMS_EBOM_Report.csv

### Options:

      -r, --EBOMReport=PATH      PATH to current EBOM Report
      -e, --EMSReport=PATH       PATH to current eMS EBOM Report
      -p, --PrevEMSReport=PATH   At least one PATH to previous eMS EBOM Report
      -d, --DSList[=PATH]        Optional: PATH to DS list output
      -h, -?, --help             Shows this help message

## Attributions:
- <div>App icon made by <a href="https://www.flaticon.com/authors/gregor-cresnar" title="Gregor Cresnar">Gregor Cresnar</a> from <a href="https://www.flaticon.com/" title="Flaticon">www.flaticon.com</a></div>
- Font Awesome Free 5.15.3 by @fontawesome - https://fontawesome.com  
License - https://fontawesome.com/license/free (Icons: CC BY 4.0, Fonts: SIL OFL 1.1, Code: MIT License)
