Aryaman BMS – Complete All India Location Seed

1. Download the official “All India Pincode Directory till last month” CSV from data.gov.in.
2. Rename it to:
   all_india_pincode_directory.csv
3. Place it at:
   C:/AryamanBMS/Data/all_india_pincode_directory.csv
   or change the LOAD DATA LOCAL INFILE path in the SQL file.
4. Ensure these tables already exist:
   TableState
   TableCity
   TablePincode
5. Enable local file import if MySQL blocks it:
   SET GLOBAL local_infile = 1;
   Also enable “OPT_LOCAL_INFILE=1” in MySQL Workbench connection settings.
6. Run:
   SEED_ALL_INDIA_LOCATION_FROM_CSV.sql

Important:
The official postal dataset reliably contains State and District fields, not a clean nationwide City master. Therefore DistrictName is stored in TableCity.CityName. Pin codes are stored with OfficeName as AreaName.

This is safer and more maintainable than embedding roughly 165,000 postal rows directly in a hand-written SQL file.
