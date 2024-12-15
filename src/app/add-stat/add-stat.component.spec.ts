import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddStatComponent } from './add-stat.component';

describe('AddStatComponent', () => {
  let component: AddStatComponent;
  let fixture: ComponentFixture<AddStatComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AddStatComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AddStatComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
