namespace MiniOrm.Migrations.Migrations;
                                                                             
            public class Migration_20260327115248_InitialCreate : IMigration
            {
                public string Name => "20260327115248_InitialCreate";
                                                                             
                public string Up() => @"
                    CREATE TABLE patients (id SERIAL PRIMARY KEY, first_name VARCHAR(100) NOT NULL, last_name VARCHAR(100) NOT NULL, oib VARCHAR(11) NOT NULL UNIQUE, date_of_birth TIMESTAMP NOT NULL);
        CREATE TABLE medical_records (id SERIAL PRIMARY KEY, patient_id INTEGER NOT NULL REFERENCES patients(id), blood_type VARCHAR(5) NOT NULL, allergies VARCHAR(255), chronic_diseases VARCHAR(255) NOT NULL);
        CREATE TABLE examinations (id SERIAL PRIMARY KEY, patient_id INTEGER NOT NULL REFERENCES patients(id), examination_type INTEGER NOT NULL, date TIMESTAMP NOT NULL, diagnosis VARCHAR(255), notes VARCHAR(255));
        CREATE TABLE medications (id SERIAL PRIMARY KEY, name VARCHAR(100) NOT NULL, manufacturer VARCHAR(100) NOT NULL, price DECIMAL NOT NULL);
        CREATE TABLE prescriptions (id SERIAL PRIMARY KEY, examination_id INTEGER NOT NULL REFERENCES examinations(id), medication_id INTEGER NOT NULL REFERENCES medications(id), dosage VARCHAR(255) NOT NULL, frequency VARCHAR(255) NOT NULL, duration_days INTEGER NOT NULL);                                                  
                ";                                                          
             
                public string Down() => @"                                  
                    DROP TABLE IF EXISTS prescriptions CASCADE;
        DROP TABLE IF EXISTS medications CASCADE;
        DROP TABLE IF EXISTS examinations CASCADE;
        DROP TABLE IF EXISTS medical_records CASCADE;
        DROP TABLE IF EXISTS patients CASCADE;
                ";
            }
            