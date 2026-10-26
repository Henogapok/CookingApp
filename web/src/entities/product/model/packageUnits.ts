/** Единица упаковки. Id — из seed справочника MeasurementUnits (ReferenceIds на сервере). */
export interface PackageUnit {
  id: string;
  label: string;
  /** Сколько граммов / миллилитров в одной единице; null — штука (вес берём из каталога). */
  size: number | null;
  /** Объём — по умолчанию у ингредиентов в мл. */
  volume: boolean;
}

export const packageUnits: PackageUnit[] = [
  { id: '00000003-0000-0000-0000-000000000001', label: 'г', size: 1, volume: false },
  { id: '00000003-0000-0000-0000-000000000008', label: 'кг', size: 1000, volume: false },
  { id: '00000003-0000-0000-0000-000000000002', label: 'мл', size: 1, volume: true },
  { id: '00000003-0000-0000-0000-000000000009', label: 'л', size: 1000, volume: true },
  { id: '00000003-0000-0000-0000-000000000003', label: 'шт', size: null, volume: false },
];

/** Миллилитры в справочнике единиц: ингредиенты в мл (молоко, масло) по умолчанию продаются литрами. */
export const milliliterUnitId = '00000003-0000-0000-0000-000000000002';
