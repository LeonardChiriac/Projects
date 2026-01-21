-- Host: localhost
-- Generation Time: March 16, 2023 at 12:24 PM
-- Name: localhost
-- Pass: null

-- Astea sunt decat niste setari, vezi si tu ce fac
SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET time_zone = "+00:00";
SET character_set_client = utf8mb4;

--
-- Database: `taxe`
--

DROP DATABASE IF EXISTS `taxe`; -- Asta sterge un basa de date in caz ca este facut
CREATE DATABASE IF NOT EXISTS `taxe`; -- Asta creaza un basa de date in caz ca nu exista
USE `taxe`; -- Asta face ca basa de date respectiv sa fie folosit

-- --------------------------------------------------------

--
-- Structura tabelului pentru `taxenfo`
--

DROP TABLE IF EXISTS `taxeInfo`; -- Asta sterge tabelul
-- Aici creaza un tabel cu numele "taxeInfo" in caz ca nu exista. Tabelul este creat cu valorile
-- id(un int care este unsigned ne nul care creste automat)
-- Produs(care este un string aka un varchar cu o lungime maxima de 256 care de asmenea este ne nul si are valoarea default '' care este un string gol
-- Valoarea_Produs(care este un float ne nul cu valoarea default 0
-- Valoarea_Totala(care este un float ne nul cu valoarea default 0
CREATE TABLE IF NOT EXISTS `taxeInfo` (
  `id` int UNSIGNED NOT NULL AUTO_INCREMENT,
  `Produs` varchar(256) NOT NULL DEFAULT '',
  `Valoare_Produs` float NOT NULL DEFAULT 0,
  `Valoare_Totala` float NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`) -- Asta seteaza "id" ca primary key
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 AUTO_INCREMENT=1 ; -- Astea ded asemenea sunt setari pentru tabel

-- Aici doar inseram in "taxeInfo" valorile din paranteze(valorile sunt in ordinea coloanelor, adica primul este id-ul, al doilea numele produsului, al trilea este pretul si al patrulea este pretul total dupa taxe
INSERT INTO `taxeInfo` VALUES (0, 'Ceapa galbena', 3.5, 0);
INSERT INTO `taxeInfo` VALUES (1, 'Ulei floare-soarelui', 11.3, 0);
INSERT INTO `taxeInfo` VALUES (2, 'Zahar alb', 4.4, 0);
INSERT INTO `taxeInfo` VALUES (3, 'Unt', 10.3, 0);
INSERT INTO `taxeInfo` VALUES (4, 'Lapte vaca', 5.8, 0);
INSERT INTO `taxeInfo` VALUES (5, 'Faina grau', 4.9, 0);
INSERT INTO `taxeInfo` VALUES (6, 'Paste fainoase', 6.5, 0);
INSERT INTO `taxeInfo` VALUES (7, 'Orez', 8.9, 0);
INSERT INTO `taxeInfo` VALUES (8, 'Pate porc conserva', 3.1, 0);
INSERT INTO `taxeInfo` VALUES (9, 'Carne porc', 22.9, 0);
INSERT INTO `taxeInfo` VALUES (10, 'Vin', 11.47, 0);
INSERT INTO `taxeInfo` VALUES (11, 'Malai', 4.8, 0);
INSERT INTO `taxeInfo` VALUES (12, 'Paine alba feli', 2.4, 0);
INSERT INTO `taxeInfo` VALUES (13, 'Cartofi albi', 2.8, 0);
INSERT INTO `taxeInfo` VALUES (14, 'Carne pasare', 23.8, 0);
INSERT INTO `taxeInfo` VALUES (15, 'Carne porc conserva', 6.9, 0);
INSERT INTO `taxeInfo` VALUES (16, 'Salam Victoria', 34.2, 0);
INSERT INTO `taxeInfo` VALUES (17, 'Mere Golden', 2.6, 0);
INSERT INTO `taxeInfo` VALUES (18, 'Portocale', 4.6, 0);
INSERT INTO `taxeInfo` VALUES (19, 'Rosii proaspete', 7.1, 0);
INSERT INTO `taxeInfo` VALUES (20, 'Oua', 4.25, 0);
INSERT INTO `taxeInfo` VALUES (21, 'Paine neagra', 7.48, 0);
INSERT INTO `taxeInfo` VALUES (22, 'Pulpe de pui', 11.25, 0);
INSERT INTO `taxeInfo` VALUES (23, 'Ulei', 5.71, 0);
INSERT INTO `taxeInfo` VALUES (24, 'Morcovi', 6.6, 0);
INSERT INTO `taxeInfo` VALUES (25, 'Vinete', 13.24, 0);
INSERT INTO `taxeInfo` VALUES (26, 'Caserola de ciuperci', 7.92, 0);
INSERT INTO `taxeInfo` VALUES (27, 'Ceapa', 6.6, 0);
INSERT INTO `taxeInfo` VALUES (28, 'Salata verde', 3.49, 0);
INSERT INTO `taxeInfo` VALUES (29, 'Lamaie', 1.71, 0);
INSERT INTO `taxeInfo` VALUES (30, 'Banane', 7.48, 0);
INSERT INTO `taxeInfo` VALUES (31, 'Kiwi', 1.72, 0);
INSERT INTO `taxeInfo` VALUES (32, 'Sos paste Braila', 8.81, 0);
INSERT INTO `taxeInfo` VALUES (33, 'Paste', 2.17, 0);
INSERT INTO `taxeInfo` VALUES (34, 'Mozzarella', 2.43, 0);
INSERT INTO `taxeInfo` VALUES (35, 'Rosii intregi in sos', 5.53, 0);
INSERT INTO `taxeInfo` VALUES (36, 'Pepsi 2l', 5.71, 0);
INSERT INTO `taxeInfo` VALUES (37, 'Cutie cereale', 4.2, 0);
INSERT INTO `taxeInfo` VALUES (38, 'Esenta de rom', 2.17, 0);
INSERT INTO `taxeInfo` VALUES (39, 'Zahar vanilat', 1.1, 0);
INSERT INTO `taxeInfo` VALUES (40, 'Cutie de ceai', 7.92, 0);

-- Aici o sa explic functiile din c#
-- Prima este "select * from taxe.taxeInfo"
-- Ce face comanda asta este ca selecteaza tot("*" inseamna tot) din taxe.taxeInfo(taxe este basa de date si taxeInfo este tabelul)
-- A doua este "insert into taxe.taxeInfo(id, Produs,Valoare_Produs) values('', '', '');
-- Functia asta doar insereaza valorile in taxe.taxeInfo
-- In c# la values in paranteze arata ciudat ca este formatat diferit, trebuie conectate string-uri intre ele folosind + dar in acelasi timp trebuie pastrate si "'" ghilimele singure de la characters. Ii mai greu de exlicat si nu stiu
-- A treia este "update taxe.taxeInfo set Produs='', etc where id='';
-- Asta doar actualizeaza un tabel dintr-o baza de date setand valori la functiile scrise
-- "where id=''" arata comenzii unde trebuie sa modifice. Inloc de id se poate pune oricare coloana atat timp cat este dat valoarea la care rand sa modifice
-- Si ultima comanda este "delete from taxe.taxeInfo where id=''"
-- Asta doar sterge o valoare dintr-un tabel. "id" dinou este point-erul unde sa stearga din tabel






